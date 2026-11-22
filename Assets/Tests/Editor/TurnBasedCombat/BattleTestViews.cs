using System.Collections.Generic;
using NUnit.Framework;
using RaggedBanner.Combat.Unity;
using UnityEngine;

namespace RaggedBanner.Combat.Tests
{
    static class BattleTestViews
    {
        public static HexCharacterView CreateCharacterView(string name, List<GameObject> objects,
            List<BattleAnimationFixture> fixtures)
        {
            GameObject root = new GameObject(name);
            objects.Add(root);
            HexCharacterView view = root.AddComponent<HexCharacterView>();
            GameObject model = new GameObject(name + " Model");
            model.transform.SetParent(root.transform, false);
            BattleAnimationFixture fixture = new BattleAnimationFixture(model);
            fixtures.Add(fixture);
            fixture.Attach(view);
            return view;
        }

        public static Material CreateRangeMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            Assert.That(shader, Is.Not.Null);
            return new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        public static void DisposeFixtures(List<BattleAnimationFixture> fixtures)
        {
            for (int i = fixtures.Count - 1; i >= 0; i--)
            {
                fixtures[i].Dispose();
            }

            fixtures.Clear();
        }
    }
}
