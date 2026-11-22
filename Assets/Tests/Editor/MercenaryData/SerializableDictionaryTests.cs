using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace RaggedBanner.MercenaryData.Tests
{
    public sealed class SerializableDictionaryTests
    {
        [Test]
        public void LookupRequiresValidationAndRetainsSourceOrder()
        {
            SerializableDictionary<string, int> dictionary = SerializableDictionaryTestUtility.Create(
                new KeyValuePair<string, int>("second", 20),
                new KeyValuePair<string, int>("first", 10));

            Assert.That(dictionary.TryGetValue("second", out int value), Is.False);
            Assert.That(value, Is.Zero);
            Assert.That(dictionary.Validate(out string error), Is.True, error);
            Assert.That(error, Is.Null);
            Assert.That(dictionary.TryGetValue("second", out value), Is.True);
            Assert.That(value, Is.EqualTo(20));
            Assert.That(dictionary.TryGetValue("first", out value), Is.True);
            Assert.That(value, Is.EqualTo(10));
            Assert.That(dictionary.TryGetValue("missing", out value), Is.False);
            Assert.That(value, Is.Zero);
            Assert.That(dictionary.EntryCount, Is.EqualTo(2));
            Assert.That(dictionary.GetKey(0), Is.EqualTo("second"));
            Assert.That(dictionary.GetValue(0), Is.EqualTo(20));
            Assert.That(dictionary.GetKey(1), Is.EqualTo("first"));
            Assert.That(dictionary.GetValue(1), Is.EqualTo(10));
        }

        [Test]
        public void EmptyEntriesAreValidButNullEntriesAreRejected()
        {
            SerializableDictionary<string, int> dictionary = SerializableDictionaryTestUtility.Create<string, int>();

            Assert.That(dictionary.EntryCount, Is.Zero);
            Assert.That(dictionary.Validate(out string error), Is.True, error);
            Assert.That(dictionary.TryGetValue("missing", out _), Is.False);

            FieldInfo entries = dictionary.GetType().GetField("_entries", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(entries, Is.Not.Null);
            entries.SetValue(dictionary, null);

            Assert.That(dictionary.Validate(out error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(dictionary.TryGetValue("missing", out _), Is.False);
        }

        [Test]
        public void NullKeysAreRejectedWithoutChangingTheSource()
        {
            SerializableDictionary<string, int> dictionary = SerializableDictionaryTestUtility.Create(
                new KeyValuePair<string, int>(null, 15));

            Assert.That(dictionary.Validate(out string error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(dictionary.EntryCount, Is.EqualTo(1));
            Assert.That(dictionary.GetKey(0), Is.Null);
            Assert.That(dictionary.GetValue(0), Is.EqualTo(15));
            Assert.That(dictionary.TryGetValue("missing", out _), Is.False);
        }

        [Test]
        public void GenericDictionaryAllowsNullValues()
        {
            SerializableDictionary<int, string> dictionary = SerializableDictionaryTestUtility.Create(
                new KeyValuePair<int, string>(1, null));

            Assert.That(dictionary.Validate(out string error), Is.True, error);
            Assert.That(dictionary.TryGetValue(1, out string value), Is.True);
            Assert.That(value, Is.Null);
        }

        [Test]
        public void DuplicateKeysPreserveBothValuesAcrossSerialization()
        {
            SerializableDictionary<string, int> dictionary = SerializableDictionaryTestUtility.Create(
                new KeyValuePair<string, int>("same", 10),
                new KeyValuePair<string, int>("same", 20));

            Assert.That(dictionary.Validate(out string error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(dictionary.TryGetValue("same", out _), Is.False);

            dictionary.OnBeforeSerialize();
            SerializableDictionary<string, int> restored = JsonUtility.FromJson<SerializableDictionary<string, int>>(
                JsonUtility.ToJson(dictionary));

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.EntryCount, Is.EqualTo(2));
            Assert.That(restored.GetKey(0), Is.EqualTo("same"));
            Assert.That(restored.GetValue(0), Is.EqualTo(10));
            Assert.That(restored.GetKey(1), Is.EqualTo("same"));
            Assert.That(restored.GetValue(1), Is.EqualTo(20));
            Assert.That(restored.Validate(out error), Is.False);
            Assert.That(restored.TryGetValue("same", out _), Is.False);
        }

        [Test]
        public void JsonRoundTripPreservesEntriesAndRequiresFreshValidation()
        {
            SerializableDictionary<string, int> dictionary = SerializableDictionaryTestUtility.Create(
                new KeyValuePair<string, int>("warrior", 10),
                new KeyValuePair<string, int>("rogue", 8));
            string sourceJson = JsonUtility.ToJson(dictionary);

            Assert.That(dictionary.Validate(out string error), Is.True, error);
            Assert.That(JsonUtility.ToJson(dictionary), Is.EqualTo(sourceJson));

            SerializableDictionary<string, int> restored = JsonUtility.FromJson<SerializableDictionary<string, int>>(sourceJson);

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.EntryCount, Is.EqualTo(2));
            Assert.That(restored.GetKey(0), Is.EqualTo("warrior"));
            Assert.That(restored.GetValue(0), Is.EqualTo(10));
            Assert.That(restored.GetKey(1), Is.EqualTo("rogue"));
            Assert.That(restored.GetValue(1), Is.EqualTo(8));
            Assert.That(restored.TryGetValue("warrior", out _), Is.False);
            Assert.That(restored.Validate(out error), Is.True, error);
            Assert.That(restored.TryGetValue("warrior", out int value), Is.True);
            Assert.That(value, Is.EqualTo(10));
        }

        [Test]
        public void InvalidationBlocksOldLookupsUntilTheSourceIsRevalidated()
        {
            SerializableDictionary<string, int> dictionary = SerializableDictionaryTestUtility.Create(
                new KeyValuePair<string, int>("warrior", 10));
            Assert.That(dictionary.Validate(out string error), Is.True, error);
            Assert.That(dictionary.TryGetValue("warrior", out _), Is.True);

            SerializableDictionaryTestUtility.AddEntry(dictionary, "rogue", 8);
            dictionary.Invalidate();

            Assert.That(dictionary.TryGetValue("warrior", out int value), Is.False);
            Assert.That(value, Is.Zero);
            Assert.That(dictionary.TryGetValue("rogue", out _), Is.False);
            Assert.That(dictionary.Validate(out error), Is.True, error);
            Assert.That(dictionary.TryGetValue("warrior", out value), Is.True);
            Assert.That(value, Is.EqualTo(10));
            Assert.That(dictionary.TryGetValue("rogue", out value), Is.True);
            Assert.That(value, Is.EqualTo(8));
        }

        [Test]
        public void FailedRevalidationNeverExposesThePreviousCache()
        {
            SerializableDictionary<string, int> dictionary = SerializableDictionaryTestUtility.Create(
                new KeyValuePair<string, int>("warrior", 10));
            Assert.That(dictionary.Validate(out string error), Is.True, error);
            Assert.That(dictionary.TryGetValue("warrior", out _), Is.True);

            SerializableDictionaryTestUtility.AddEntry(dictionary, "warrior", 99);

            Assert.That(dictionary.Validate(out error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(dictionary.TryGetValue("warrior", out int value), Is.False);
            Assert.That(value, Is.Zero);
            Assert.That(dictionary.EntryCount, Is.EqualTo(2));
            Assert.That(dictionary.GetValue(0), Is.EqualTo(10));
            Assert.That(dictionary.GetValue(1), Is.EqualTo(99));
        }

        [Test]
        public void SerializationCallbacksPreserveTheSourceAndInvalidateAfterDeserialization()
        {
            SerializableDictionary<string, int> dictionary = SerializableDictionaryTestUtility.Create(
                new KeyValuePair<string, int>("warrior", 10));
            Assert.That(dictionary.Validate(out string error), Is.True, error);

            dictionary.OnBeforeSerialize();

            Assert.That(dictionary.TryGetValue("warrior", out int value), Is.True);
            Assert.That(value, Is.EqualTo(10));

            dictionary.OnAfterDeserialize();

            Assert.That(dictionary.TryGetValue("warrior", out value), Is.False);
            Assert.That(value, Is.Zero);
            Assert.That(dictionary.EntryCount, Is.EqualTo(1));
            Assert.That(dictionary.GetKey(0), Is.EqualTo("warrior"));
            Assert.That(dictionary.GetValue(0), Is.EqualTo(10));
            Assert.That(dictionary.Validate(out error), Is.True, error);
            Assert.That(dictionary.TryGetValue("warrior", out value), Is.True);
            Assert.That(value, Is.EqualTo(10));
        }
    }

    static class SerializableDictionaryTestUtility
    {
        internal static SerializableDictionary<TKey, TValue> Create<TKey, TValue>(
            params KeyValuePair<TKey, TValue>[] entries)
        {
            SerializableDictionary<TKey, TValue> dictionary = new SerializableDictionary<TKey, TValue>();

            for (int i = 0; i < entries.Length; i++)
            {
                AddEntry(dictionary, entries[i].Key, entries[i].Value);
            }

            return dictionary;
        }

        internal static void AddEntry<TKey, TValue>(SerializableDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            FieldInfo entriesField = dictionary.GetType().GetField("_entries", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(entriesField, Is.Not.Null);
            IList entries = (IList)entriesField.GetValue(dictionary);
            Assert.That(entries, Is.Not.Null);
            Type entryType = entriesField.FieldType.GetGenericArguments()[0];
            object entry = Activator.CreateInstance(entryType);
            FieldInfo keyField = entryType.GetField("_key", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo valueField = entryType.GetField("_value", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(keyField, Is.Not.Null);
            Assert.That(valueField, Is.Not.Null);
            keyField.SetValue(entry, key);
            valueField.SetValue(entry, value);
            entries.Add(entry);
        }
    }
}
