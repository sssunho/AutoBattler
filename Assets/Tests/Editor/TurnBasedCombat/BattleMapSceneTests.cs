using System.Reflection;
using NUnit.Framework;
using RaggedBanner.BattleMaps;
using RaggedBanner.Combat.Unity;
using RaggedBanner.HexGrid.Unity;
using RaggedBanner.Mercenaries;
using RaggedBanner.MercenaryData;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleMapSceneTests
    {
        Scene _scene;

        [SetUp]
        public void SetUp()
        {
            _scene = EditorSceneManager.NewPreviewScene();
        }

        [TearDown]
        public void TearDown()
        {
            BattleStartupCancellationProbe.Target = null;

            if (_scene.IsValid())
            {
                EditorSceneManager.ClosePreviewScene(_scene);
            }
        }

        [Test]
        public void MapSceneRequiresExactlyOneActiveGrid()
        {
            Assert.That(BattleMapSceneSession.TryFindMap(_scene, out _, out _), Is.False);
            HexMapView map = CreateObject("Map").AddComponent<HexMapView>();
            Assert.That(BattleMapSceneSession.TryFindMap(_scene, out HexMapView found, out string error), Is.True, error);
            Assert.That(found, Is.SameAs(map));
            map.enabled = false;
            Assert.That(BattleMapSceneSession.TryFindMap(_scene, out _, out _), Is.False);
            map.enabled = true;
            CreateObject("Second map").AddComponent<HexMapView>();
            Assert.That(BattleMapSceneSession.TryFindMap(_scene, out _, out _), Is.False);
        }

        [Test]
        public void MapSceneRejectsBattleComponentsEvenWhenInactive()
        {
            CreateObject("Map").AddComponent<HexMapView>();
            GameObject character = CreateObject("Old scene character");
            character.AddComponent<HexCharacterView>();
            character.SetActive(false);
            Assert.That(BattleMapSceneSession.TryFindMap(_scene, out _, out string error), Is.False);
            StringAssert.Contains("battle units", error);
        }

        [Test]
        public void MapSceneRejectsActiveCameraButAllowsAnInactiveAuthoringCamera()
        {
            CreateObject("Map").AddComponent<HexMapView>();
            Camera camera = CreateObject("Camera").AddComponent<Camera>();
            Assert.That(BattleMapSceneSession.TryFindMap(_scene, out _, out _), Is.False);
            camera.enabled = false;
            Assert.That(BattleMapSceneSession.TryFindMap(_scene, out _, out string error), Is.True, error);
        }

        [Test]
        public void MapSceneRejectsCollapsedGridTransform()
        {
            HexMapView map = CreateObject("Map").AddComponent<HexMapView>();
            map.transform.localScale = new Vector3(1f, 0f, 1f);
            Assert.That(BattleMapSceneSession.TryFindMap(_scene, out _, out string error), Is.False);
            StringAssert.Contains("transform scale", error);
        }

        [Test]
        public void ControllerShutdownDoesNotReleaseAnUnownedBattle()
        {
            BattleRunner runner = CreateObject("Runner").AddComponent<BattleRunner>();
            SetField(runner, "_initializationMode", BattleInitializationMode.External);
            BattleSimulation existing = new BattleSimulation(4, 4, new BattleRules());
            SetField(runner, "_simulation", existing);
            BattleStartController controller = CreateObject("Controller").AddComponent<BattleStartController>();
            SetField(controller, "_runner", runner);
            SetField(controller, "_phase", BattleStartPhase.Running);
            typeof(BattleStartController).GetMethod("Shutdown", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, null);
            Assert.That(runner.IsInitialized, Is.True);
            Assert.That(typeof(BattleRunner).GetField("_simulation", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(runner), Is.SameAs(existing));
        }

        [Test]
        [Category("ProjectContent")]
        public void SavedMinePlacementsInitializeWithTheAuthoredClassPrefabs()
        {
            BattleMapDefinition definition = AssetDatabase.LoadAssetAtPath<BattleMapDefinition>(
                "Assets/GameData/BattleMaps/Mine_BattleMap.asset");
            MercenaryDataList classData = AssetDatabase.LoadAssetAtPath<MercenaryDataList>(
                "Assets/GameData/Mercenaries/MercenaryDataList.asset");
            Assert.That(definition, Is.Not.Null);
            Assert.That(classData, Is.Not.Null);
            BattleClassViewPrefab[] prefabs = new BattleClassViewPrefab[3];

            for (int i = 0; i < prefabs.Length; i++)
            {
                MercenaryClass unitClass = (MercenaryClass)i;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/Combat/" + unitClass + ".prefab");
                Assert.That(prefab, Is.Not.Null, unitClass.ToString());
                prefabs[i] = new BattleClassViewPrefab(unitClass, prefab.GetComponent<HexCharacterView>());
            }

            HexMapView map = CreateObject("Map").AddComponent<HexMapView>();
            SetField(map, "_width", 28);
            SetField(map, "_height", 28);
            MercenaryClass[] party = { MercenaryClass.Warrior, MercenaryClass.Rogue, MercenaryClass.Archer, MercenaryClass.Warrior };
            Assert.That(BattleMapPreparation.TryPrepare(definition, party, classData, prefabs, 28, 28, 4,
                out BattleSpawnInput[] inputs, out int skipped, out string error), Is.True, error);
            Assert.That(skipped, Is.EqualTo(Mathf.Max(0, party.Length - definition.StartPoints.Count)));
            BattleRunner runner = CreateObject("Runner").AddComponent<BattleRunner>();
            SetField(runner, "_initializationMode", BattleInitializationMode.External);

            try
            {
                Assert.That(runner.TryInitializeBattle(map, inputs, 12345, out error), Is.True, error);
                Assert.That(runner.State, Is.EqualTo(BattleState.Running));

                for (int i = 0; i < inputs.Length; i++)
                {
                    Assert.That(runner.TryGetCharacter(i + 1, out HexCharacter character), Is.True);
                    Assert.That(character.Coordinate, Is.EqualTo(inputs[i].Coordinate));
                    Assert.That(character.Facing, Is.EqualTo(inputs[i].Facing));
                    Assert.That(character.Health, Is.EqualTo(inputs[i].Stats.MaxHealth));
                }
            }
            finally
            {
                runner.ShutdownBattle();
            }
        }

        [Test]
        [Category("ProjectContent")]
        public void ControllerDisabledDuringInstantiationDoesNotReviveControlsOrKeepTheBattle()
        {
            BattleMapDefinition definition = AssetDatabase.LoadAssetAtPath<BattleMapDefinition>(
                "Assets/GameData/BattleMaps/Mine_BattleMap.asset");
            MercenaryDataList classData = AssetDatabase.LoadAssetAtPath<MercenaryDataList>(
                "Assets/GameData/Mercenaries/MercenaryDataList.asset");
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Combat/Warrior.prefab");
            Assert.That(definition, Is.Not.Null);
            Assert.That(classData, Is.Not.Null);
            Assert.That(source, Is.Not.Null);
            GameObject template = Object.Instantiate(source);
            SceneManager.MoveGameObjectToScene(template, _scene);
            template.AddComponent<BattleStartupCancellationProbe>();
            HexCharacterView prefab = template.GetComponent<HexCharacterView>();
            HexMapView map = CreateObject("Map").AddComponent<HexMapView>();
            SetField(map, "_width", 28);
            SetField(map, "_height", 28);
            BattleRunner runner = CreateObject("Runner").AddComponent<BattleRunner>();
            SetField(runner, "_initializationMode", BattleInitializationMode.External);
            BattleInputController input = CreateObject("Input").AddComponent<BattleInputController>();
            input.enabled = false;
            GameObject hud = CreateObject("HUD");
            hud.AddComponent<Canvas>();
            hud.SetActive(false);
            Camera camera = CreateObject("Camera").AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(0f, 12f, -10f), Quaternion.Euler(45f, 0f, 0f));
            BattleStartController controller = CreateObject("Controller").AddComponent<BattleStartController>();
            SetField(controller, "_runner", runner);
            SetField(controller, "_input", input);
            SetField(controller, "_phase", BattleStartPhase.Loading);
            SetField(controller, "_battleHud", hud);
            SetField(controller, "_battleCamera", camera);
            SetField(controller, "_requestedDefinition", definition);
            SetField(controller, "_requestedScenePath", definition.ScenePath);
            SetField(controller, "_requestedParty", new[] { MercenaryClass.Warrior });
            SetField(controller, "_requestedClassData", classData);
            SetField(controller, "_requestedPrefabs", new[]
            {
                new BattleClassViewPrefab(MercenaryClass.Warrior, prefab),
                new BattleClassViewPrefab(MercenaryClass.Rogue, prefab),
                new BattleClassViewPrefab(MercenaryClass.Archer, prefab)
            });
            BattleStartupCancellationProbe.Target = controller;

            try
            {
                typeof(BattleStartController).GetMethod("OnMapReady", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(controller, new object[] { map });
                Assert.That(BattleStartupCancellationProbe.Target, Is.Null, controller.Status);
                Assert.That(controller.IsRunning, Is.False);
                Assert.That(runner.IsInitialized, Is.False);
                Assert.That(runner.transform.childCount, Is.Zero);
                Assert.That(input.enabled, Is.False);
                Assert.That(hud.activeSelf, Is.False);
            }
            finally
            {
                runner.ShutdownBattle();
            }
        }

        GameObject CreateObject(string name)
        {
            GameObject instance = new GameObject(name);
            SceneManager.MoveGameObjectToScene(instance, _scene);
            return instance;
        }

        static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
