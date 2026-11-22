using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RaggedBanner.Events
{
    sealed class EventJsonReader
    {
        readonly string _source;

        EventJsonReader(string source)
        {
            _source = source;
        }

        internal static EventDefinition Read(string source, string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new FormatException(source + ": Event JSON is empty.");
            }

            var parser = new EventJsonReader(source);
            try
            {
                using (var input = new StringReader(json))
                using (var reader = new JsonTextReader(input))
                {
                    reader.DateParseHandling = DateParseHandling.None;
                    reader.MaxDepth = 32;
                    var settings = new JsonLoadSettings
                    {
                        DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
                        CommentHandling = CommentHandling.Load
                    };
                    JToken root = JToken.Load(reader, settings);
                    if (reader.Read())
                    {
                        throw parser.Invalid(root, "Unexpected content after the event object.");
                    }

                    return parser.ReadEvent(root);
                }
            }
            catch (JsonException exception)
            {
                throw new FormatException(source + ": " + exception.Message, exception);
            }
        }

        EventDefinition ReadEvent(JToken token)
        {
            JObject data = Object(token, "id", "title", "description", "arcId", "goalId",
                "eventType", "arcStage", "conditions", "resolution");
            Null(data["arcId"]);
            Null(data["goalId"]);
            Null(data["arcStage"]);
            string type = Text(data["eventType"]);
            EventType eventType;
            if (type == "Opening")
            {
                eventType = EventType.Opening;
            }
            else if (type == "Normal")
            {
                eventType = EventType.Normal;
            }
            else
            {
                throw Invalid(data["eventType"], "Only Opening and standalone Normal events are supported.");
            }

            EventConditions conditions = ReadConditions(data["conditions"]);
            return new EventDefinition(Id(data["id"]), Text(data["title"]), Text(data["description"]),
                eventType, conditions, ReadResolution(data["resolution"], conditions));
        }

        EventConditions ReadConditions(JToken token)
        {
            JObject data = Object(token, "requiredTags", "forbiddenTags", "fameMin", "fameMax",
                "reputationMin", "reputationMax", "moneyMin", "moneyMax",
                "resolvedEventCountMin", "resolvedEventCountMax", "locations");
            string[] required = ConditionIds(data["requiredTags"]);
            string[] forbidden = ConditionIds(data["forbiddenTags"]);
            Disjoint(required, forbidden, token);
            int? fameMin = Bound(data["fameMin"], 0, 10);
            int? fameMax = Bound(data["fameMax"], 0, 10);
            int? reputationMin = Bound(data["reputationMin"], -3, 3);
            int? reputationMax = Bound(data["reputationMax"], -3, 3);
            int? moneyMin = Bound(data["moneyMin"], 0, int.MaxValue);
            int? moneyMax = Bound(data["moneyMax"], 0, int.MaxValue);
            int? countMin = Bound(data["resolvedEventCountMin"], 0, int.MaxValue);
            int? countMax = Bound(data["resolvedEventCountMax"], 0, int.MaxValue);
            Range(fameMin, fameMax, data["fameMin"]);
            Range(reputationMin, reputationMax, data["reputationMin"]);
            Range(moneyMin, moneyMax, data["moneyMin"]);
            Range(countMin, countMax, data["resolvedEventCountMin"]);

            JArray locations = Array(data["locations"]);
            if (locations.Count == 0)
            {
                throw Invalid(locations, "At least one location is required.");
            }

            int[] ids = new int[locations.Count];
            var seen = new HashSet<int>();
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = Integer(locations[i]);
                if (ids[i] <= 0 || !seen.Add(ids[i]))
                {
                    throw Invalid(locations[i], "Location IDs must be positive and unique.");
                }
            }

            return new EventConditions(required, forbidden, fameMin, fameMax,
                reputationMin, reputationMax, moneyMin, moneyMax, countMin, countMax, ids);
        }

        EventResolution ReadResolution(JToken token, EventConditions eventConditions)
        {
            if (!(token is JObject data) || data["type"] == null)
            {
                throw Invalid(token, "A resolution object with a type is required.");
            }

            string type = Text(data["type"]);
            if (type == "Automatic")
            {
                Object(data, "type", "result");
                EventResult result = ReadResult(data["result"]);
                ValidateCost(result, eventConditions.MoneyMin ?? 0, data["result"]);
                return new EventResolution(EventResolutionType.Automatic, System.Array.Empty<EventChoice>(), result);
            }

            if (type != "Choice")
            {
                throw Invalid(data["type"], "Resolution type must be Choice or Automatic.");
            }

            Object(data, "type", "choices");
            JArray choices = Array(data["choices"]);
            var results = new EventChoice[choices.Count];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            bool hasUnconditionalChoice = false;
            for (int i = 0; i < results.Length; i++)
            {
                JObject choice = Object(choices[i], "id", "text", "conditions", "result");
                string id = Id(choice["id"]);
                if (id == "auto" || !ids.Add(id))
                {
                    throw Invalid(choice["id"], "Choice IDs must be unique and cannot use the reserved ID auto.");
                }

                JObject conditions = Object(choice["conditions"], "requiredTags", "forbiddenTags", "moneyMin");
                string[] required = ConditionIds(conditions["requiredTags"]);
                string[] forbidden = ConditionIds(conditions["forbiddenTags"]);
                Disjoint(required, forbidden, conditions);
                Disjoint(required, eventConditions.ForbiddenTags, conditions);
                Disjoint(forbidden, eventConditions.RequiredTags, conditions);
                int? minimum = Bound(conditions["moneyMin"], 0, int.MaxValue);
                Range(minimum, eventConditions.MoneyMax, conditions["moneyMin"]);
                EventResult result = ReadResult(choice["result"]);
                ValidateCost(result, Math.Max(minimum ?? 0, eventConditions.MoneyMin ?? 0), choice["result"]);
                results[i] = new EventChoice(id, Text(choice["text"]),
                    new EventChoiceConditions(required, forbidden, minimum), result);
                if (required.Length == 0 && forbidden.Length == 0 && (!minimum.HasValue || minimum.Value == 0))
                {
                    hasUnconditionalChoice = true;
                }
            }

            if (!hasUnconditionalChoice)
            {
                throw Invalid(choices, "This prototype requires at least one unconditional choice.");
            }

            return new EventResolution(EventResolutionType.Choice, results, null);
        }

        EventResult ReadResult(JToken token)
        {
            JObject data = Object(token, "text", "battle", "effects");
            if (data["battle"].Type != JTokenType.Boolean)
            {
                throw Invalid(data["battle"], "A boolean is required.");
            }

            JObject effects = Object(data["effects"], "addTags", "removeTags", "fameChange",
                "reputationChange", "moneyChange", "revealGoalIds", "arcTransition");
            string[] added = Ids(effects["addTags"]);
            string[] removed = Ids(effects["removeTags"]);
            Disjoint(added, removed, effects);
            if (Ids(effects["revealGoalIds"]).Length != 0)
            {
                throw Invalid(effects["revealGoalIds"], "Goal reveal effects are not supported by this prototype.");
            }

            Null(effects["arcTransition"]);
            return new EventResult(Text(data["text"]), (bool)data["battle"],
                new EventEffects(added, removed, Integer(effects["fameChange"]),
                    Integer(effects["reputationChange"]), Integer(effects["moneyChange"])));
        }

        JObject Object(JToken token, params string[] fields)
        {
            if (!(token is JObject data))
            {
                throw Invalid(token, "An object is required.");
            }

            var expected = new HashSet<string>(fields, StringComparer.Ordinal);
            foreach (JProperty property in data.Properties())
            {
                if (!expected.Remove(property.Name))
                {
                    throw Invalid(property, "Unknown field.");
                }
            }

            if (expected.Count != 0)
            {
                throw Invalid(data, "Missing required fields: " + string.Join(", ", expected) + ".");
            }

            return data;
        }

        JArray Array(JToken token)
        {
            if (!(token is JArray array))
            {
                throw Invalid(token, "An array is required.");
            }

            return array;
        }

        string Text(JToken token)
        {
            if (token.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)token))
            {
                throw Invalid(token, "A nonempty string is required.");
            }

            return (string)token;
        }

        string Id(JToken token)
        {
            string id = Text(token);
            for (int i = 0; i < id.Length; i++)
            {
                if (char.IsWhiteSpace(id[i]))
                {
                    throw Invalid(token, "IDs cannot contain whitespace.");
                }
            }

            return id;
        }

        string[] Ids(JToken token)
        {
            JArray array = Array(token);
            string[] result = new string[array.Count];
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = Id(array[i]);
                if (!seen.Add(result[i]))
                {
                    throw Invalid(array[i], "IDs must be unique within an array.");
                }
            }

            return result;
        }

        string[] ConditionIds(JToken token)
        {
            return token.Type == JTokenType.Null ? System.Array.Empty<string>() : Ids(token);
        }

        int Integer(JToken token)
        {
            if (token.Type != JTokenType.Integer)
            {
                throw Invalid(token, "A 32-bit integer is required.");
            }

            try
            {
                return checked((int)token);
            }
            catch (OverflowException)
            {
                throw Invalid(token, "Integer is outside the 32-bit range.");
            }
        }

        int? Bound(JToken token, int minimum, int maximum)
        {
            if (token.Type == JTokenType.Null)
            {
                return null;
            }

            int value = Integer(token);
            if (value < minimum || value > maximum)
            {
                throw Invalid(token, "Condition bound is outside the supported state range.");
            }

            return value;
        }

        void Null(JToken token)
        {
            if (token.Type != JTokenType.Null)
            {
                throw Invalid(token, "This prototype requires this unsupported field to be null.");
            }
        }

        void Range(int? minimum, int? maximum, JToken token)
        {
            if (minimum.HasValue && maximum.HasValue && minimum.Value > maximum.Value)
            {
                throw Invalid(token, "Minimum cannot exceed maximum.");
            }
        }

        void Disjoint(IReadOnlyList<string> first, IReadOnlyList<string> second, JToken token)
        {
            var ids = new HashSet<string>(first, StringComparer.Ordinal);
            for (int i = 0; i < second.Count; i++)
            {
                if (ids.Contains(second[i]))
                {
                    throw Invalid(token, "Conflicting tag lists contain " + second[i] + ".");
                }
            }
        }

        void ValidateCost(EventResult result, int guaranteedMoney, JToken token)
        {
            if ((long)guaranteedMoney + result.Effects.MoneyChange < 0)
            {
                throw Invalid(token, "A money cost must be covered by the event or choice minimum.");
            }
        }

        FormatException Invalid(JToken token, string message)
        {
            string path = token == null || token.Path.Length == 0 ? "$" : "$." + token.Path;
            return new FormatException(_source + " at " + path + ": " + message);
        }
    }
}
