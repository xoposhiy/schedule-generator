using System;
using Domain;
using Domain.Algorithms.Solvers;
using NUnit.Framework;
using static Testing.ObjectMother;

namespace Testing.SolversTests
{
    [TestFixture]
    public class BeamSolverTests
    {
        [Test]
        [Explicit]
        public void BeamBetterThanGreedy()
        {
            var estimator = DomainExtensions.GetDefaultCombinedEstimator();
            var greedy = new GreedySolver(estimator, AutumnRequisition, ClassRooms, new(42), 5);
            var beam = new BeamSolver(estimator, AutumnRequisition, ClassRooms, greedy, 5);

            var (beamSchedule, beamScore) = beam.GetSolution(TimeSpan.Zero);
            var (greedySchedule, greedyScore) = greedy.GetSolution(TimeSpan.Zero);

            Assert.GreaterOrEqual(greedySchedule.NotUsedMeetings.Count, beamSchedule.NotUsedMeetings.Count);
            Assert.GreaterOrEqual(beamScore, greedyScore);
        }
    }
}