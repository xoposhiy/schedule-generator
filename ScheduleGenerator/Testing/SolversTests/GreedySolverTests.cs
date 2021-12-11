using System;
using Domain;
using Domain.Algorithms.Solvers;
using NUnit.Framework;
using static Testing.ObjectMother;

namespace Testing.SolversTests
{
    [TestFixture]
    public class GreedySolverTests
    {
        [Test]
        public void FindFullPlaceSolution()
        {
            var estimator = DomainExtensions.GetDefaultCombinedEstimator();
            var greedy = new GreedySolver(estimator, FullMondayRequisition, ClassroomsRequisitions, new(42), 3);
            var solution = greedy.GetSolution(TimeSpan.Zero);
            Assert.AreEqual(0, solution.Schedule.NotUsedMeetings.Count);
        }
    }
}