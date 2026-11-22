using System;
using NUnit.Framework;
using RaggedBanner.GameState;

namespace RaggedBanner.WorldMap.Tests
{
    public sealed class WorldMapTravelTests
    {
        [TestCase(1)]
        [TestCase(3)]
        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(99)]
        public void CannotMoveToCurrentNonAdjacentOrUnknownLocation(int destination)
        {
            WorldMapTravel travel = CreateTravel();

            Assert.That(travel.CanTravelTo(destination), Is.False);
            Assert.That(travel.TryBeginTravel(destination), Is.False);
            Assert.That(travel.CurrentLocationId, Is.EqualTo(1));
            Assert.That(travel.IsMoving, Is.False);
        }

        [Test]
        public void CurrentLocationChangesOnlyOnArrivalAndSecondRequestCannotReplaceTrip()
        {
            WorldMapTravel travel = CreateTravel();

            Assert.That(travel.TryBeginTravel(2), Is.True);
            Assert.That(travel.CurrentLocationId, Is.EqualTo(1));
            Assert.That(travel.DestinationLocationId, Is.EqualTo(2));
            Assert.That(travel.ActiveRoad.Id, Is.EqualTo(10));
            Assert.That(travel.IsMoving, Is.True);
            Assert.That(travel.CanTravelTo(4), Is.False);
            Assert.That(travel.TryBeginTravel(4), Is.False);
            Assert.That(travel.TryBeginTravel(2), Is.False);
            Assert.That(travel.DestinationLocationId, Is.EqualTo(2));

            Assert.That(travel.TryCompleteTravel(out WorldMapArrival arrival), Is.True);
            Assert.That(arrival.FromLocationId, Is.EqualTo(1));
            Assert.That(arrival.ToLocationId, Is.EqualTo(2));
            Assert.That(arrival.RoadId, Is.EqualTo(10));
            Assert.That(travel.CurrentLocationId, Is.EqualTo(2));
            Assert.That(travel.IsMoving, Is.False);
            Assert.That(travel.DestinationLocationId, Is.Zero);
            Assert.That(travel.ActiveRoad.Id, Is.Zero);
            Assert.That(travel.CanTravelTo(3), Is.True);
            Assert.That(travel.CanTravelTo(4), Is.False);
        }

        [Test]
        public void IdleAndRepeatedCompletionDoNotProduceAnotherArrival()
        {
            WorldMapTravel travel = CreateTravel();

            Assert.That(travel.TryCompleteTravel(out _), Is.False);
            Assert.That(travel.TryBeginTravel(2), Is.True);
            Assert.That(travel.TryCompleteTravel(out _), Is.True);
            Assert.That(travel.TryCompleteTravel(out WorldMapArrival duplicate), Is.False);
            Assert.That(duplicate.ToLocationId, Is.Zero);
            Assert.That(travel.CurrentLocationId, Is.EqualTo(2));
        }

        [Test]
        public void RoadAllowsRepeatedReturnTripsWithDirectionalArrivalInformation()
        {
            WorldMapTravel travel = CreateTravel();

            for (int trip = 0; trip < 4; trip++)
            {
                int origin = trip % 2 == 0 ? 1 : 2;
                int destination = trip % 2 == 0 ? 2 : 1;
                Assert.That(travel.CanTravelTo(destination), Is.True);
                Assert.That(travel.TryBeginTravel(destination), Is.True);
                Assert.That(travel.TryCompleteTravel(out WorldMapArrival arrival), Is.True);
                Assert.That(arrival.FromLocationId, Is.EqualTo(origin));
                Assert.That(arrival.ToLocationId, Is.EqualTo(destination));
                Assert.That(arrival.RoadId, Is.EqualTo(10));
            }
        }

        [Test]
        public void InputCollectionsCannotChangeAnExistingGraph()
        {
            int[] locations = { 1, 2, 3 };
            WorldMapRoad[] roads = { new WorldMapRoad(10, 1, 2, 5) };
            WorldMapGraph graph = new WorldMapGraph(locations, roads);
            locations[0] = 99;
            roads[0] = new WorldMapRoad(20, 2, 3, 5);

            Assert.That(graph.ContainsLocation(1), Is.True);
            Assert.That(graph.ContainsLocation(99), Is.False);
            Assert.That(graph.TryGetRoad(2, 1, out WorldMapRoad reverse), Is.True);
            Assert.That(reverse.Id, Is.EqualTo(10));
            Assert.That(graph.TryGetRoad(2, 3, out _), Is.False);
        }

        [Test]
        public void RejectsInvalidLocationSetsAndStartingLocation()
        {
            WorldMapRoad[] noRoads = Array.Empty<WorldMapRoad>();
            Assert.Throws<ArgumentException>(() => new WorldMapGraph(Array.Empty<int>(), noRoads));
            Assert.Throws<ArgumentException>(() => new WorldMapGraph(new[] { 1, 1 }, noRoads));
            Assert.Throws<ArgumentException>(() => new WorldMapGraph(new[] { 0, 1 }, noRoads));
            WorldMapGraph graph = new WorldMapGraph(new[] { 1, 2 }, noRoads);
            Assert.Throws<ArgumentException>(() => new WorldMapTravel(graph, 99, 100));
            Assert.That(new WorldMapTravel(graph, 1, 100).CanTravelTo(2), Is.False);
        }

        [Test]
        public void RejectsDanglingSelfDuplicateAndInvalidRoads()
        {
            int[] locations = { 1, 2, 3 };
            Assert.Throws<ArgumentException>(() => new WorldMapGraph(locations,
                new[] { new WorldMapRoad(1, 1, 99, 5) }));
            Assert.Throws<ArgumentException>(() => new WorldMapGraph(locations,
                new[] { new WorldMapRoad(1, 1, 1, 5) }));
            Assert.Throws<ArgumentException>(() => new WorldMapGraph(locations,
                new[] { new WorldMapRoad(0, 1, 2, 5) }));
            Assert.Throws<ArgumentException>(() => new WorldMapGraph(locations,
                new[] { new WorldMapRoad(1, 1, 2, 5), new WorldMapRoad(1, 2, 3, 5) }));
            Assert.Throws<ArgumentException>(() => new WorldMapGraph(locations,
                new[] { new WorldMapRoad(1, 1, 2, 5), new WorldMapRoad(2, 2, 1, 5) }));
        }

        [Test]
        public void OnlyAcceptedDepartureChargesTimeOnce()
        {
            WorldMapTravel travel = CreateTravel();
            Assert.That(travel.RemainingTime, Is.EqualTo(100));
            Assert.That(travel.TryBeginTravel(99), Is.False);
            Assert.That(travel.RemainingTime, Is.EqualTo(100));
            Assert.That(travel.TryBeginTravel(2), Is.True);
            Assert.That(travel.RemainingTime, Is.EqualTo(95));
            Assert.That(travel.TryBeginTravel(2), Is.False);
            Assert.That(travel.TryCompleteTravel(out _), Is.True);
            Assert.That(travel.TryCompleteTravel(out _), Is.False);
            Assert.That(travel.RemainingTime, Is.EqualTo(95));
        }

        [TestCase(3, -2)]
        [TestCase(5, 0)]
        public void PositiveTimeAllowsFinalTripButNoDepartureAfterTimeRunsOut(int initialTime, long expectedTime)
        {
            WorldMapGraph graph = new WorldMapGraph(new[] { 1, 2 },
                new[] { new WorldMapRoad(1, 1, 2, 5) });
            WorldMapTravel travel = new WorldMapTravel(graph, 1, initialTime);
            Assert.That(travel.CanTravelTo(2), Is.True);
            Assert.That(travel.TryBeginTravel(2), Is.True);
            Assert.That(travel.RemainingTime, Is.EqualTo(expectedTime));
            Assert.That(travel.TryCompleteTravel(out _), Is.True);
            Assert.That(travel.CurrentLocationId, Is.EqualTo(2));
            Assert.That(travel.CanTravelTo(1), Is.False);
            Assert.That(travel.TryBeginTravel(1), Is.False);
            Assert.That(travel.RemainingTime, Is.EqualTo(expectedTime));
        }

        [TestCase(0)]
        [TestCase(5)]
        public void ZeroRemainingTimePreventsDepartureEvenOnFreeRoad(int travelCost)
        {
            WorldMapGraph graph = new WorldMapGraph(new[] { 1, 2 },
                new[] { new WorldMapRoad(1, 1, 2, travelCost) });
            WorldMapTravel travel = new WorldMapTravel(graph, 1, 0);

            Assert.That(travel.CanTravelTo(2), Is.False);
            Assert.That(travel.TryBeginTravel(2), Is.False);
            Assert.That(travel.IsMoving, Is.False);
            Assert.That(travel.RemainingTime, Is.Zero);
        }

        [Test]
        public void SharedExplorationStateReceivesCostAndArrivalWithoutLosingArea()
        {
            WorldMapGraph graph = new WorldMapGraph(new[] { 1, 2 },
                new[] { new WorldMapRoad(1, 1, 2, 5) });
            ExplorationState state = new ExplorationState(new LocationKey(7, 1), 3);
            WorldMapTravel travel = new WorldMapTravel(graph, state);

            Assert.That(travel.TryBeginTravel(2), Is.True);
            Assert.That(state.RemainingExpeditionTime, Is.EqualTo(-2));
            Assert.That(state.CurrentLocation, Is.EqualTo(new LocationKey(7, 1)));
            Assert.That(travel.TryBeginTravel(2), Is.False);
            Assert.That(state.RemainingExpeditionTime, Is.EqualTo(-2));
            Assert.That(travel.TryCompleteTravel(out _), Is.True);
            Assert.That(state.CurrentLocation, Is.EqualTo(new LocationKey(7, 2)));
            Assert.That(state.RemainingExpeditionTime, Is.EqualTo(-2));
            Assert.That(travel.TryCompleteTravel(out _), Is.False);
            Assert.That(travel.TryBeginTravel(1), Is.False);
        }

        [Test]
        public void TravelReadsCurrentLocationAndTimeFromSharedState()
        {
            WorldMapGraph graph = new WorldMapGraph(new[] { 1, 2, 3 },
                new[] { new WorldMapRoad(1, 1, 2, 5), new WorldMapRoad(2, 2, 3, 5) });
            ExplorationState state = new ExplorationState(new LocationKey(1, 1), 100);
            WorldMapTravel travel = new WorldMapTravel(graph, state);
            state.CurrentLocation = new LocationKey(1, 2);
            state.RemainingExpeditionTime = 0;

            Assert.That(travel.CurrentLocationId, Is.EqualTo(2));
            Assert.That(travel.RemainingTime, Is.Zero);
            Assert.That(travel.TryBeginTravel(3), Is.False);
            state.RemainingExpeditionTime = 6;
            Assert.That(travel.TryBeginTravel(3), Is.True);
            Assert.That(state.RemainingExpeditionTime, Is.EqualTo(1));
            Assert.That(travel.TryCompleteTravel(out _), Is.True);
            Assert.That(state.CurrentLocation, Is.EqualTo(new LocationKey(1, 3)));
        }

        [Test]
        public void SharedStateMustHaveValidAreaLocationAndInitialTime()
        {
            WorldMapGraph graph = new WorldMapGraph(new[] { 1, 2 }, Array.Empty<WorldMapRoad>());

            Assert.Throws<ArgumentNullException>(() => new WorldMapTravel(graph, null));
            Assert.Throws<ArgumentException>(() => new WorldMapTravel(graph,
                new ExplorationState(new LocationKey(0, 1), 100)));
            Assert.Throws<ArgumentException>(() => new WorldMapTravel(graph,
                new ExplorationState(new LocationKey(1, 99), 100)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new WorldMapTravel(graph,
                new ExplorationState(new LocationKey(1, 1), -1)));
        }

        [Test]
        public void UsesConfiguredCostsIncludingZeroAndRejectsNegativeSettings()
        {
            WorldMapGraph graph = new WorldMapGraph(new[] { 1, 2, 3 }, new[]
            {
                new WorldMapRoad(1, 1, 2, 0),
                new WorldMapRoad(2, 2, 3, 17)
            });
            WorldMapTravel travel = new WorldMapTravel(graph, 1, 23);
            Assert.That(travel.TryBeginTravel(2), Is.True);
            Assert.That(travel.RemainingTime, Is.EqualTo(23));
            Assert.That(travel.TryCompleteTravel(out _), Is.True);
            Assert.That(travel.TryBeginTravel(3), Is.True);
            Assert.That(travel.RemainingTime, Is.EqualTo(6));
            Assert.Throws<ArgumentOutOfRangeException>(() => new WorldMapRoad(1, 1, 2, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new WorldMapTravel(graph, 1, -1));
        }

        static WorldMapTravel CreateTravel()
        {
            return new WorldMapTravel(new WorldMapGraph(new[] { 1, 2, 3, 4 }, new[]
            {
                new WorldMapRoad(10, 1, 2, 5),
                new WorldMapRoad(20, 2, 3, 5),
                new WorldMapRoad(30, 1, 4, 5)
            }), 1, 100);
        }
    }
}
