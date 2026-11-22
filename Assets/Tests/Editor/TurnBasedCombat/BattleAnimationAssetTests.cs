using System.Collections.Generic;
using NUnit.Framework;
using RaggedBanner.Animations;
using RaggedBanner.Combat.Unity;
using UnityEditor;
using UnityEngine;

namespace RaggedBanner.Combat.Tests
{
    [Category("ProjectContent")]
    public sealed class BattleAnimationAssetTests
    {
        [Test]
        public void CombatPrefabsUseSeparateValidKeyedProfilesPreservingOriginalDefinitionData()
        {
            string[] classes = { "Warrior", "Archer", "Rogue" };
            string[] roles = { "Idle", "Move", "Attack" };
            string[] keys = { "idle", "move", "attack1" };
            string[] clipNames = { "HumanM@CombatIdle2H01", "HumanM@Run01_Forward", "HumanM@Attack2H01" };
            HashSet<CharacterAnimationProfile> profiles = new HashSet<CharacterAnimationProfile>();
            HashSet<CharacterAnimationEntry> entries = new HashSet<CharacterAnimationEntry>();
            AnimationClip[] originalClips = new AnimationClip[roles.Length];

            for (int i = 0; i < classes.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Combat/" + classes[i] + ".prefab");
                Assert.That(prefab, Is.Not.Null);
                HexCharacterView view = prefab.GetComponent<HexCharacterView>();
                Assert.That(view, Is.Not.Null);
                Animator animator = prefab.GetComponentInChildren<Animator>(true);
                Assert.That(animator, Is.Not.Null);
                Assert.That(animator.runtimeAnimatorController, Is.Null);
                Assert.That(animator.avatar != null && animator.avatar.isValid && animator.avatar.isHuman, Is.True);
                SerializedObject data = new SerializedObject(view);
                CharacterAnimationProfile profile = (CharacterAnimationProfile)data.FindProperty("_animationProfile").objectReferenceValue;
                Assert.That(profile, Is.Not.Null);
                Assert.That(profile.ValidateForPlayback(out string error), Is.True, error);
                Assert.That(profiles.Add(profile), Is.True);
                Assert.That(AssetDatabase.GetAssetPath(profile),
                    Is.EqualTo("Assets/GameData/Animations/Profiles/" + classes[i] + "AnimationProfile.asset"));
                Assert.That(profile.Animations.Count, Is.EqualTo(3));

                for (int role = 0; role < roles.Length; role++)
                {
                    Assert.That(profile.TryGetAnimation(keys[role], out CharacterAnimationEntry entry), Is.True);
                    Assert.That(entries.Add(entry), Is.True);
                    AnimationDefinition original = AssetDatabase.LoadAssetAtPath<AnimationDefinition>(
                        "Assets/GameData/Animations/Definitions/" + classes[i] + roles[role] + ".asset");
                    Assert.That(original, Is.Not.Null);
                    Assert.That(entry.Clip, Is.SameAs(original.Clip));
                    Assert.That(entry.AuthoredFrameRate, Is.EqualTo(original.AuthoredFrameRate));
                    Assert.That(entry.AuthoredLength, Is.EqualTo(original.AuthoredLength));
                    Assert.That(original.Events.Count, Is.Zero);
                    Assert.That(entry.Events.Count, Is.EqualTo(role == 2 ? 1 : 0));

                    if (role == 2)
                    {
                        Assert.That(entry.Events[0].EventKind, Is.EqualTo(AnimationEventKind.AttackHit));
                        Assert.That(entry.Events[0].Frame, Is.InRange(0,
                            Mathf.RoundToInt(entry.AuthoredLength * entry.AuthoredFrameRate)));
                    }

                    Assert.That(entry.Clip.name, Is.EqualTo(clipNames[role]));
                    Assert.That(entry.Clip.humanMotion, Is.True);
                    Assert.That(entry.Clip.isLooping, Is.EqualTo(role != 2));

                    if (i == 0)
                    {
                        originalClips[role] = entry.Clip;
                    }
                    else
                    {
                        Assert.That(entry.Clip, Is.SameAs(originalClips[role]));
                    }
                }
            }

            Assert.That(profiles.Count, Is.EqualTo(3));
            Assert.That(entries.Count, Is.EqualTo(9));
        }
    }
}
