using System.Linq;
using Domain;
using Domain.Algorithms;
using Domain.Algorithms.Estimators;
using NUnit.Framework;
using static Testing.ObjectMother;

namespace Testing.EstimatorsTests
{
    [TestFixture]
    public class CombinedEstimatorTests
    {
        [Test]
        public static void EstimatorsSameDeltaTest()
        {
            var estimator = new MeetingsPerDayEstimator();
            // TODO krutovsky: run through all estimators
            AssertSameScoreDelta(estimator);
        }

        public static void AssertSameScoreDelta(IEstimator estimator)
        {
            var schedule = new Schedule(FullMondayRequisition, ClassRooms);
            var previousScheduleScore = estimator.Estimate(schedule);

            while (true)
            {
                var meetingToAdd = schedule.GetMeetingsToAdd().FirstOrDefault();
                if (meetingToAdd == null) break;
                var meetingScoreDelta = estimator.EstimateMeetingToAdd(schedule, meetingToAdd);
                schedule.AddMeeting(meetingToAdd);
                var currentScheduleScore = estimator.Estimate(schedule);
                var scheduleScoreDelta = currentScheduleScore - previousScheduleScore;
                Assert.AreEqual(meetingScoreDelta, scheduleScoreDelta, 0.0001);
                previousScheduleScore = currentScheduleScore;
            }
        }
    }
}