using System.Linq;
using Domain;
using Domain.Algorithms.Estimators;
using NUnit.Framework;
using static Testing.ObjectMother;

namespace Testing.EstimatorsTests
{
    [TestFixture]
    public class PriorityMeetingsEstimatorTests
    {
        private readonly PriorityMeetingsEstimator estimator = new();

        [Test]
        public void ZeroWhenAllUnnecessary()
        {
            var schedule = new Schedule(CalculusRequisition, ClassRooms);
            Assert.Zero(estimator.Estimate(schedule));
        }

        [Test]
        public void PenaltyWhenNecessaryNotPlaced()
        {
            var schedule = new Schedule(NecessaryMeetings, ClassRooms);
            Assert.Negative(estimator.Estimate(schedule));

            while (schedule.NotUsedMeetings.Count != 0)
            {
                var meeting = schedule.GetMeetingsToAdd().First();
                schedule.AddMeeting(meeting);
            }

            Assert.Zero(estimator.Estimate(schedule));
        }
    }
}