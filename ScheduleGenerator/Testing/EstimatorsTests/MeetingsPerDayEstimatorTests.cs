using System.Linq;
using Domain;
using Domain.Algorithms.Estimators;
using NUnit.Framework;
using static Testing.ScheduleLibTests.ObjectMother;

namespace Testing.EstimatorsTests
{
    [TestFixture]
    public class MeetingsPerDayEstimatorTests
    {
        private readonly MeetingsPerDayEstimator estimator = new();

        [Test]
        public void SingleMeetingSingleDayTest()
        {
            var schedule = new Schedule(OsRequisition, ClassRooms);
            var osLecture = schedule.GetMeetingsToAdd().First();
            schedule.AddMeeting(osLecture);
            var score = estimator.Estimate(schedule);
            Assert.AreEqual(-1, score);
        }

        [Test]
        public void FewMeetingsSingleDayTest()
        {
            var schedule = new Schedule(CalculusRequisition, ClassRooms);
            while (schedule.NotUsedMeetings.Count != 0)
            {
                var score = estimator.Estimate(schedule);
                Assert.Greater(0, score);
                var meeting = schedule.GetMeetingsToAdd().First();
                schedule.AddMeeting(meeting);
            }

            var finalScore = estimator.Estimate(schedule);
            Assert.AreEqual(0, finalScore);
        }

        [Test]
        public void LotOfMeetingsSingleDay()
        {
            var schedule = new Schedule(FullMondayRequisition, ClassRooms);
            
            while (schedule.NotUsedMeetings.Count != 0)
            {
                var meeting = schedule.GetMeetingsToAdd().First();
                schedule.AddMeeting(meeting);
            }

            var finalScore = estimator.Estimate(schedule);
            Assert.Greater(0, finalScore);
        }
    }
}