using System.Collections.Generic;
using AutoBattler.HexGrid;
using NUnit.Framework;

namespace RaggedBanner.Combat.Tests
{
    public sealed class HexPathfinderTests
    {
        [Test]
        public void EveryPairInSmallMapHasShortestAdjacentPath()
        {
            HexPathfinder pathfinder = new HexPathfinder();
            List<HexCoordinate> path = new List<HexCoordinate>();
            BattleBoard board = new BattleBoard(5, 4);

            for (int startR = 0; startR < 4; startR++)
            {
                for (int startQ = 0; startQ < 5; startQ++)
                {
                    for (int endR = 0; endR < 4; endR++)
                    {
                        for (int endQ = 0; endQ < 5; endQ++)
                        {
                            HexCoordinate start = new HexCoordinate(startQ, startR);
                            HexCoordinate end = new HexCoordinate(endQ, endR);
                            Assert.That(pathfinder.TryFindPath(board, start, end, path), Is.True);
                            Assert.That(path.Count, Is.EqualTo(start.DistanceTo(end)));
                            HexCoordinate previous = start;

                            for (int i = 0; i < path.Count; i++)
                            {
                                Assert.That(previous.DistanceTo(path[i]), Is.EqualTo(1));
                                Assert.That(path[i].Q, Is.InRange(0, 4));
                                Assert.That(path[i].R, Is.InRange(0, 3));
                                previous = path[i];
                            }

                            Assert.That(previous, Is.EqualTo(end));
                        }
                    }
                }
            }
        }

        [TestCase(0, 4, 0, 0)]
        [TestCase(4, 0, 0, 0)]
        [TestCase(4, 4, -1, 0)]
        [TestCase(4, 4, 4, 0)]
        [TestCase(4, 4, 0, 4)]
        public void InvalidDestinationClearsPreviousPath(int width, int height, int q, int r)
        {
            HexPathfinder pathfinder = new HexPathfinder();
            List<HexCoordinate> path = new List<HexCoordinate>();
            Assert.That(pathfinder.TryFindPath(new BattleBoard(4, 4), default, new HexCoordinate(3, 3), path), Is.True);
            Assert.That(pathfinder.TryFindPath(new BattleBoard(width, height), default, new HexCoordinate(q, r), path), Is.False);
            Assert.That(path, Is.Empty);
            Assert.That(pathfinder.TryFindPath(new BattleBoard(1, 1), default, default, path), Is.True);
            Assert.That(path, Is.Empty);
        }

        [Test]
        public void InvalidStartAndNullOutputAreRejected()
        {
            HexPathfinder pathfinder = new HexPathfinder();
            List<HexCoordinate> path = new List<HexCoordinate>();
            Assert.That(pathfinder.TryFindPath(new BattleBoard(2, 2), new HexCoordinate(-1, 0), default, path), Is.False);
            Assert.That(pathfinder.TryFindPath(new BattleBoard(2, 2), default, default, null), Is.False);
        }
    }
}
