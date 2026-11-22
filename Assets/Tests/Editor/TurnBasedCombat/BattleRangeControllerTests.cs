using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RaggedBanner.Combat.Unity;
using RaggedBanner.HexGrid;
using RaggedBanner.HexGrid.Unity;
using RaggedBanner.Mercenaries;
using UnityEngine;

namespace RaggedBanner.Combat.Tests
{
    public sealed class BattleRangeControllerTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        readonly List<BattleAnimationFixture> _animations = new List<BattleAnimationFixture>();
        BattleRangeController _controller;
        BattleRunner _runner;
        BattleInputController _input;
        BattleSimulation _simulation;
        HexMapView _map;
        HexCharacterView _playerView;
        Camera _camera;
        Material _material;

        [SetUp]
        public void SetUp()
        {
            _material = BattleTestViews.CreateRangeMaterial();
            _map = CreateObject("Map").AddComponent<HexMapView>();
            _runner = CreateObject("Runner").AddComponent<BattleRunner>();
            _camera = CreateObject("Camera").AddComponent<Camera>();
            _input = CreateObject("Input").AddComponent<BattleInputController>();
            SetField(_input, "_runner", _runner);
            SetField(_input, "_inputCamera", _camera);
            _playerView = BattleTestViews.CreateCharacterView("Player", _objects, _animations);
            BindSimulation(CreateSimulation(new HexCoordinate(3, 1)));
            _controller = CreateObject("Range Controller").AddComponent<BattleRangeController>();
            SetField(_controller, "_runner", _runner);
            SetField(_controller, "_input", _input);
            SetField(_controller, "_material", _material);
        }

        [TearDown]
        public void TearDown()
        {
            if (_controller != null)
            {
                Object.DestroyImmediate(_controller.gameObject);
            }

            if (_runner != null)
            {
                _runner.ShutdownBattle();
            }

            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
            BattleTestViews.DisposeFixtures(_animations);

            if (_material != null)
            {
                Object.DestroyImmediate(_material);
            }
        }

        [Test]
        public void InitialRangeUsesTheMapTransformAndOwnsNoCanvasOrCollider()
        {
            _map.transform.SetPositionAndRotation(new Vector3(8f, 0.5f, -4f), Quaternion.Euler(0f, 35f, 0f));
            _map.transform.localScale = Vector3.one * 1.2f;
            Tick();
            BattleRangeMeshView view = GetView();
            Assert.That(view.transform.parent, Is.EqualTo(_map.transform));
            Assert.That(view.transform.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(view.transform.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(view.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.True);
            Assert.That(view.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(_material));
            Assert.That(view.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThan(0));
            Assert.That(view.GetComponent<Canvas>(), Is.Null);
            Assert.That(view.GetComponent<Collider>(), Is.Null);
        }

        [Test]
        public void DestroyedRangeViewRebuildsWithUnchangedDomainState()
        {
            Tick();
            BattleRangeMeshView previousView = GetView();
            Object.DestroyImmediate(previousView.gameObject);
            Tick();
            BattleRangeMeshView replacement = GetView();
            Assert.That(replacement.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThan(0));
            Assert.That(replacement.GetComponent<MeshRenderer>().enabled, Is.True);
        }

        [Test]
        public void RepeatedMovementHidesAndRestoresTheSameMeshAndMaterial()
        {
            Tick();
            BattleRangeMeshView view = GetView();
            MeshRenderer renderer = view.GetComponent<MeshRenderer>();
            Mesh mesh = view.GetComponent<MeshFilter>().sharedMesh;
            HexCoordinate[] destinations =
            {
                new HexCoordinate(2, 1), new HexCoordinate(2, 2), new HexCoordinate(1, 2)
            };

            for (int i = 0; i < destinations.Length; i++)
            {
                Assert.That(_simulation.TryMove(1, destinations[i]), Is.True);
                Tick();
                Assert.That(renderer.enabled, Is.False);
                Assert.That(_simulation.TryBeginMovementStep(out _), Is.True);
                Assert.That(_simulation.TryCompleteMovementStep(), Is.True);
                Assert.That(_simulation.IsBusy, Is.False);
                Tick();
                Assert.That(GetView(), Is.SameAs(view));
                Assert.That(renderer.enabled, Is.True);
                Assert.That(renderer.sharedMaterial, Is.SameAs(_material));
                Assert.That(view.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(mesh));
                Assert.That(mesh.vertexCount, Is.GreaterThan(0));
            }
        }

        [TestCase("input")]
        [TestCase("camera")]
        [TestCase("runner-binding")]
        [TestCase("material")]
        public void UnavailableInputOrMaterialHidesAndRestoresTheRange(string unavailable)
        {
            Tick();
            BattleRangeMeshView view = GetView();
            MeshRenderer renderer = view.GetComponent<MeshRenderer>();

            switch (unavailable)
            {
                case "input":
                    _input.enabled = false;
                    break;
                case "camera":
                    _camera.enabled = false;
                    break;
                case "runner-binding":
                    SetField(_input, "_runner", null);
                    break;
                default:
                    SetField(_controller, "_material", null);
                    break;
            }

            Tick();
            Assert.That(renderer.enabled, Is.False);
            _input.enabled = true;
            _camera.enabled = true;
            SetField(_input, "_runner", _runner);
            SetField(_controller, "_material", _material);
            Tick();
            Assert.That(GetView(), Is.SameAs(view));
            Assert.That(renderer.enabled, Is.True);
            Assert.That(renderer.sharedMaterial, Is.SameAs(_material));
        }

        [Test]
        public void EnemyTurnAndCompletedBattleHideTheRange()
        {
            Tick();
            MeshRenderer renderer = GetView().GetComponent<MeshRenderer>();
            BattleTestUtility.AdvanceToTurn(_simulation, 2);
            Tick();
            Assert.That(renderer.enabled, Is.False);
            BattleTestUtility.AdvanceToTurn(_simulation, 1);
            Tick();
            Assert.That(renderer.enabled, Is.True);
            Assert.That(_simulation.RemoveUnit(2), Is.True);
            Assert.That(_simulation.TryFinalizeResult(out _), Is.True);
            Tick();
            Assert.That(renderer.enabled, Is.False);
        }

        [Test]
        public void TargetMarkersUseBoardCoordinatesAndDistinguishDirectFromApproachAttacks()
        {
            BindSimulation(CreateSimulation(new HexCoordinate(1, 2), new HexCoordinate(4, 1)));
            Tick();
            List<BattleGroundMarker> markers = (List<BattleGroundMarker>)GetField(_controller, "_markers");
            Assert.That(markers.Count, Is.EqualTo(2));
            bool foundDirect = false;
            bool foundApproach = false;

            for (int i = 0; i < markers.Count; i++)
            {
                if (markers[i].Coordinate == new HexCoordinate(1, 2))
                {
                    Assert.That(markers[i].RequiresMovement, Is.False);
                    foundDirect = true;
                }

                if (markers[i].Coordinate == new HexCoordinate(4, 1))
                {
                    Assert.That(markers[i].RequiresMovement, Is.True);
                    foundApproach = true;
                }
            }

            Assert.That(foundDirect && foundApproach, Is.True);
        }

        [Test]
        public void InsufficientActionPointsKeepAttackRangeButRemoveTargets()
        {
            Tick();
            Assert.That((List<BattleGroundMarker>)GetField(_controller, "_markers"), Is.Not.Empty);
            Assert.That(_simulation.Board.TryGetUnit(1, out HexCharacter player), Is.True);
            SetField(player, "_actionPoints", 1);
            Tick();
            Assert.That((List<BattleGroundMarker>)GetField(_controller, "_markers"), Is.Empty);
            Assert.That((List<BattleHexEdge>)GetField(_controller, "_attackEdges"), Is.Not.Empty);
            Assert.That((List<BattleHexEdge>)GetField(_controller, "_movementEdges"), Is.Not.Empty);
            Assert.That(GetView().GetComponent<MeshRenderer>().enabled, Is.True);
        }

        [Test]
        public void NewSimulationWithMatchingCountersReplacesThePreviousTargets()
        {
            Tick();
            BattleRangeMeshView view = GetView();
            uint previousVersion = _simulation.Board.OccupancyVersion;
            BattleSimulation replacement = CreateSimulation(new HexCoordinate(1, 4));
            Assert.That(replacement.Board.OccupancyVersion, Is.EqualTo(previousVersion));
            BindSimulation(replacement);
            Tick();
            List<BattleGroundMarker> markers = (List<BattleGroundMarker>)GetField(_controller, "_markers");
            Assert.That(markers.Count, Is.EqualTo(1));
            Assert.That(markers[0].Coordinate, Is.EqualTo(new HexCoordinate(1, 4)));
            Assert.That(GetField(_controller, "_cachedSimulation"), Is.SameAs(replacement));
            Assert.That(GetView(), Is.SameAs(view));
        }

        [TestCase(1, true)]
        [TestCase(3, false)]
        public void AttackDisplayUsesTheSelectedUnitsSkillCost(int cost, bool affordable)
        {
            BattleSimulation simulation = new BattleSimulation(_map.Width, _map.Height,
                new BattleRules(startingActionPoints: 2), 11);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(1, 1),
                new BattleTestUnit(MercenaryClass.Warrior, 30, 3, 0, 1, 0, initiative: 20, actionPointCost: cost),
                HexDirection.East, out HexCharacter player), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(2, 1),
                BattleTestUtility.CreateStats(), HexDirection.West, out _), Is.True);
            BattleTestUtility.StartTurn(simulation, player.Id);
            BindSimulation(simulation);
            Tick();

            List<BattleGroundMarker> markers = (List<BattleGroundMarker>)GetField(_controller, "_markers");
            Assert.That(markers.Count, Is.EqualTo(affordable ? 1 : 0));
            Assert.That((List<BattleHexEdge>)GetField(_controller, "_attackEdges"), Is.Not.Empty);
            Color expected = affordable ? new Color(1f, 0.44f, 0.44f, 1f) : new Color(0.67f, 0.67f, 0.67f, 1f);
            Assert.That(GetView().GetComponent<MeshFilter>().sharedMesh.colors, Has.Member(expected));
        }

        [Test]
        public void MapDestructionReleasesTheMeshAndRebindingCreatesOneNewView()
        {
            Tick();
            BattleRangeMeshView oldView = GetView();
            Mesh oldMesh = oldView.GetComponent<MeshFilter>().sharedMesh;
            Invoke(oldView, "OnDestroy");
            Object.DestroyImmediate(_map.gameObject);
            Tick();
            Assert.That(oldView == null, Is.True);
            Assert.That(oldMesh == null, Is.True);
            Assert.That(GetField(_controller, "_hasCache"), Is.False);
            _map = CreateObject("Replacement Map").AddComponent<HexMapView>();
            BindSimulation(CreateSimulation(new HexCoordinate(3, 1)));
            Tick();
            Assert.That(GetView().transform.parent, Is.EqualTo(_map.transform));
            Assert.That(_map.GetComponentsInChildren<BattleRangeMeshView>(true).Length, Is.EqualTo(1));
            Assert.That(GetView().GetComponent<MeshRenderer>().enabled, Is.True);
        }

        [Test]
        public void DisablingAndReenablingControllerReleasesAndRecreatesItsOwnedView()
        {
            Tick();
            BattleRangeMeshView oldView = GetView();
            Mesh oldMesh = oldView.GetComponent<MeshFilter>().sharedMesh;
            _controller.enabled = false;
            Invoke(_controller, "OnDisable");
            Assert.That(oldView == null, Is.True);
            Assert.That(oldMesh == null, Is.True);
            Assert.That(_map.GetComponentsInChildren<BattleRangeMeshView>(true), Is.Empty);
            _controller.enabled = true;
            Invoke(_controller, "OnEnable");
            Tick();
            Assert.That(GetView().GetComponent<MeshRenderer>().enabled, Is.True);
            Assert.That(_map.GetComponentsInChildren<BattleRangeMeshView>(true).Length, Is.EqualTo(1));
        }

        [Test]
        public void ShutdownEventHidesTheRangeBeforeTheNextFrame()
        {
            Tick();
            MeshRenderer renderer = GetView().GetComponent<MeshRenderer>();
            _runner.ShutdownBattle();
            Assert.That(renderer.enabled, Is.False);
            Assert.That(GetField(_controller, "_hasCache"), Is.False);
        }

        [Test]
        public void HoverPathKeepsTheRangeOutlineAndMarksTheDestination()
        {
            _camera.orthographic = true;
            _camera.orthographicSize = 10f;
            _camera.pixelRect = new Rect(0f, 0f, 800f, 600f);
            _camera.transform.position = new Vector3(0f, 10f, 0f);
            _camera.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            SetField(_input, "_characterLayerMask", (LayerMask)0);
            HexCoordinate destination = new HexCoordinate(1, 4);
            Vector3 screen = _camera.WorldToScreenPoint(_map.AxialToWorld(destination));
            Assert.That(_input.TryUpdateMovementPreview(new Vector2(screen.x, screen.y)), Is.True);
            Tick();
            BattleRangeMeshView pathView = GetMovementPathView();
            Mesh mesh = pathView.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(GetView().GetComponent<MeshRenderer>().enabled, Is.True);
            Assert.That(pathView.GetComponent<MeshRenderer>().enabled, Is.True);
            Assert.That(pathView.transform.parent, Is.EqualTo(_map.transform));
            Assert.That(pathView.GetComponent<Canvas>(), Is.Null);
            Assert.That(pathView.GetComponent<Collider>(), Is.Null);
            Assert.That(mesh.vertexCount, Is.EqualTo(54));
            Assert.That(mesh.colors[0], Is.EqualTo(new Color(1f, 0.9f, 0.2f, 0.65f)));
            Assert.That(mesh.colors[18], Is.EqualTo(new Color(0.15f, 0.75f, 1f, 0.3f)));
            Assert.That((List<HexCoordinate>)GetField(_controller, "_movementLanding"),
                Is.EqualTo(new[] { destination }));
            Tick();
            Assert.That(GetMovementPathView(), Is.SameAs(pathView));
            Assert.That(pathView.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(mesh));
            Assert.That(_input.TryUpdateMovementPreview(new Vector2(float.NaN, 0f)), Is.False);
            Tick();
            Assert.That(pathView.GetComponent<MeshRenderer>().enabled, Is.False);
            Assert.That(GetView().GetComponent<MeshRenderer>().enabled, Is.True);
        }

        [Test]
        public void RemainingPathKeepsTheActiveStepUntilCompletionAndSurvivesPause()
        {
            Assert.That(_simulation.TryMove(1, new HexCoordinate(1, 4)), Is.True);
            Tick();
            BattleRangeMeshView view = GetMovementPathView();
            Mesh mesh = view.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(mesh.vertexCount, Is.EqualTo(54));
            Assert.That(_simulation.TryBeginMovementStep(out _), Is.True);
            Tick();
            Assert.That(mesh.vertexCount, Is.EqualTo(54));
            Assert.That(_simulation.TryCompleteMovementStep(), Is.True);
            Tick();
            Assert.That(mesh.vertexCount, Is.EqualTo(36));
            Assert.That((List<HexCoordinate>)GetField(_controller, "_movementPath"),
                Is.EqualTo(new[] { new HexCoordinate(1, 3), new HexCoordinate(1, 4) }));
            Assert.That(_simulation.PauseMovement(), Is.True);
            Tick();
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.True);
            Assert.That(mesh.vertexCount, Is.EqualTo(36));
            Assert.That(_simulation.ResumeMovement(), Is.True);
            Assert.That(_simulation.TryBeginMovementStep(out _), Is.True);
            _simulation.CancelAction();
            Tick();
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.False);
        }

        [Test]
        public void ApproachAttackPathDisappearsWhenTheAttackBecomesReady()
        {
            BindSimulation(CreateSimulation(new HexCoordinate(4, 1)));
            Assert.That(_simulation.TryAttack(1, 2), Is.True);
            Tick();
            BattleRangeMeshView view = GetMovementPathView();
            Assert.That(view.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.EqualTo(36));

            for (int i = 0; i < 2; i++)
            {
                Assert.That(_simulation.TryBeginMovementStep(out _), Is.True);
                Assert.That(_simulation.TryCompleteMovementStep(), Is.True);
            }

            Assert.That(_simulation.Phase, Is.EqualTo(BattleActionPhase.ReadyToAttack));
            Tick();
            Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.False);
        }

        [Test]
        public void EnemyMovementDoesNotCreateAPathView()
        {
            BattleTestUtility.AdvanceToTurn(_simulation, 2);
            Assert.That(_simulation.TryMove(2, new HexCoordinate(4, 1)), Is.True);
            Tick();
            Assert.That(((BattleRangePresentationLayer)GetField(_controller, "_movementPathLayer")).View, Is.Null);
        }

        [Test]
        public void MovementPathMeshIsReleasedOnDisableAndHiddenOnShutdown()
        {
            Assert.That(_simulation.TryMove(1, new HexCoordinate(1, 4)), Is.True);
            Tick();
            BattleRangeMeshView oldView = GetMovementPathView();
            Mesh oldMesh = oldView.GetComponent<MeshFilter>().sharedMesh;
            _controller.enabled = false;
            Invoke(_controller, "OnDisable");
            Assert.That(oldView == null, Is.True);
            Assert.That(oldMesh == null, Is.True);
            _controller.enabled = true;
            Invoke(_controller, "OnEnable");
            Tick();
            BattleRangeMeshView newView = GetMovementPathView();
            Assert.That(newView.GetComponent<MeshRenderer>().enabled, Is.True);
            _runner.ShutdownBattle();
            Assert.That(newView.GetComponent<MeshRenderer>().enabled, Is.False);
            Assert.That(((BattleRangePresentationLayer)GetField(_controller, "_movementPathLayer")).RequiresRebuild, Is.True);
        }

        [TestCase(SkillType.PullSelf, 2, 6)]
        [TestCase(SkillType.PullTarget, 2, 6)]
        [TestCase(SkillType.AssassinLeap, 1, 4)]
        [TestCase(SkillType.Ambush, 1, 3)]
        public void SelectedSkillUsesEffectiveRangeAndCancelRestoresDefault(SkillType skill, int minimum, int maximum)
        {
            BindSkillSimulation(skill);
            Tick();
            BattleRangeMeshView view = GetView();
            List<BattleHexEdge> basicEdges = new List<BattleHexEdge>((List<BattleHexEdge>)GetField(_controller, "_attackEdges"));
            List<BattleGroundMarker> basicMarkers = new List<BattleGroundMarker>((List<BattleGroundMarker>)GetField(_controller, "_markers"));

            for (int repeat = 0; repeat < 2; repeat++)
            {
                Assert.That(_input.TryBeginSkillSelection(skill), Is.True);
                Tick();
                Assert.That(GetView(), Is.SameAs(view));
                Assert.That(view.GetComponent<MeshRenderer>().enabled, Is.True);
                Assert.That((List<BattleHexEdge>)GetField(_controller, "_movementEdges"), Is.Empty);
                Assert.That((List<BattleGroundMarker>)GetField(_controller, "_markers"), Is.Empty);
                List<BattleHexEdge> edges = (List<BattleHexEdge>)GetField(_controller, "_attackEdges");
                Assert.That(edges, Is.Not.Empty);
                HashSet<HexCoordinate> cells = new HashSet<HexCoordinate>();
                for (int i = 0; i < edges.Count; i++)
                {
                    cells.Add(edges[i].Coordinate);
                }

                for (int r = 0; r < _map.Height; r++)
                {
                    for (int q = 0; q < _map.Width; q++)
                    {
                        HexCoordinate coordinate = new HexCoordinate(q, r);
                        int distance = coordinate.DistanceTo(new HexCoordinate(1, 1));
                        Assert.That(cells.Contains(coordinate), Is.EqualTo(distance >= minimum && distance <= maximum));
                    }
                }

                _input.CancelActionSelection();
                Tick();
                Assert.That((List<BattleHexEdge>)GetField(_controller, "_attackEdges"), Is.EqualTo(basicEdges));
                Assert.That((List<BattleGroundMarker>)GetField(_controller, "_markers"), Is.EqualTo(basicMarkers));
                Assert.That((List<BattleHexEdge>)GetField(_controller, "_movementEdges"), Is.Not.Empty);
            }
        }

        [Test]
        public void SelectedRangeRefreshesWhenSmokeIsCreatedAndRemoved()
        {
            BindSkillSimulation(SkillType.PullSelf);
            Assert.That(_input.TryBeginSkillSelection(SkillType.PullSelf), Is.True);
            Tick();
            List<BattleHexEdge> initial = new List<BattleHexEdge>((List<BattleHexEdge>)GetField(_controller, "_attackEdges"));
            HexCoordinate smoke = new HexCoordinate(3, 1);
            HexCoordinate behindSmoke = new HexCoordinate(4, 1);
            Assert.That(_simulation.TryApplyAirEffect(smoke, BattleAirEffect.Smoke, out _), Is.True);
            Tick();
            List<BattleHexEdge> edges = (List<BattleHexEdge>)GetField(_controller, "_attackEdges");
            Assert.That(edges.Count, Is.LessThan(initial.Count));
            for (int i = 0; i < edges.Count; i++)
            {
                Assert.That(edges[i].Coordinate, Is.Not.EqualTo(smoke));
                Assert.That(edges[i].Coordinate, Is.Not.EqualTo(behindSmoke));
            }

            Assert.That(_input.SelectedSkill, Is.EqualTo(SkillType.PullSelf));
            Assert.That(_simulation.TryClearEnvironment(smoke, false, true, out _), Is.True);
            Tick();
            Assert.That((List<BattleHexEdge>)GetField(_controller, "_attackEdges"), Is.EqualTo(initial));
        }

        void BindSkillSimulation(SkillType skill)
        {
            bool rogue = skill == SkillType.AssassinLeap || skill == SkillType.Ambush;
            MercenaryClass unitClass = rogue ? MercenaryClass.Rogue : MercenaryClass.Warrior;
            MercenarySpecialization specialization = rogue ? MercenarySpecialization.Assassin : MercenarySpecialization.Pioneer;
            BattleSimulation simulation = new BattleSimulation(_map.Width, _map.Height, randomSeed: 11);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(1, 1),
                new BattleTestUnit(unitClass, 20, 3, 3, 3, 0), HexDirection.East,
                out HexCharacter player, specialization: specialization), Is.True);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, new HexCoordinate(3, 1),
                BattleTestUtility.CreateStats(), HexDirection.West, out _), Is.True);
            BattleTestUtility.StartTurn(simulation, player.Id);
            if (skill == SkillType.Ambush)
            {
                SetField(player, "_isStealthed", true);
            }

            BindSimulation(simulation);
        }

        BattleRangeMeshView GetMovementPathView()
        {
            BattleRangeMeshView view = ((BattleRangePresentationLayer)GetField(_controller, "_movementPathLayer")).View;
            Assert.That(view, Is.Not.Null);
            return view;
        }

        BattleSimulation CreateSimulation(params HexCoordinate[] enemyCoordinates)
        {
            BattleSimulation simulation = new BattleSimulation(_map.Width, _map.Height, randomSeed: 11);
            Assert.That(simulation.TrySpawnUnit(BattleTeam.Player, new HexCoordinate(1, 1),
                BattleTestUtility.CreateStats(movementPerTurn: 3), HexDirection.East, out HexCharacter player), Is.True);

            for (int i = 0; i < enemyCoordinates.Length; i++)
            {
                Assert.That(simulation.TrySpawnUnit(BattleTeam.Enemy, enemyCoordinates[i],
                    BattleTestUtility.CreateStats(), HexDirection.West, out _), Is.True);
            }

            BattleTestUtility.StartTurn(simulation, player.Id);
            return simulation;
        }

        void BindSimulation(BattleSimulation simulation)
        {
            _simulation = simulation;
            SetField(_runner, "_mapView", _map);
            SetField(_runner, "_hexSize", _map.HexSize);
            SetField(_runner, "_simulation", simulation);
            Assert.That(simulation.Board.TryGetUnit(1, out HexCharacter player), Is.True);
            _playerView.Unbind();
            Assert.That(_playerView.TryBind(player, _map), Is.True);
            Dictionary<int, HexCharacterView> views = (Dictionary<int, HexCharacterView>)GetField(_runner, "_views");
            views.Clear();
            views.Add(player.Id, _playerView);
            Assert.That(_playerView.IsReady, Is.True);
        }

        BattleRangeMeshView GetView()
        {
            BattleRangeMeshView view = ((BattleRangePresentationLayer)GetField(_controller, "_rangeLayer")).View;
            Assert.That(view, Is.Not.Null);
            return view;
        }

        void Tick()
        {
            typeof(BattleRangeController).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_controller, null);
        }

        GameObject CreateObject(string name)
        {
            GameObject instance = new GameObject(name);
            _objects.Add(instance);
            return instance;
        }

        static object GetField(object target, string name)
        {
            return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        static void Invoke(object target, string name)
        {
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
        }
    }
}
