using System;
using Domain;
using Domain.Algorithms.Solvers;
using NUnit.Framework;
using static Testing.ObjectMother;

namespace Testing.SolversTests
{
    [TestFixture]
    public class RepeaterSolverTests
    {
        [Test]
        [Explicit]
        public void BetterThanInnerSolver()
        {
            var timeBudget = TimeSpan.FromSeconds(15);
            var estimator = DomainExtensions.GetDefaultCombinedEstimator();

            var greedy = new GreedySolver(estimator, AutumnRequisition, ClassroomsRequisitions,
                new(42), 5);
            var repeater = new RepeaterSolver(greedy);

            var (greedySchedule, greedyScore) = greedy.GetSolution(timeBudget);
            var (repeaterSchedule, repeaterScore) = repeater.GetSolution(timeBudget);

            Assert.GreaterOrEqual(greedySchedule.NotUsedMeetings.Count, repeaterSchedule.NotUsedMeetings.Count);
            Assert.GreaterOrEqual(repeaterScore, greedyScore);
        }
    }
}