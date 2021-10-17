using System.Linq;
using Domain;
using Domain.Algorithms.Estimators;
using NUnit.Framework;
using static Testing.ObjectMother;

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
                Assert.GreaterOrEqual(0, score);
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
        
        [Test]
        public void TwoBadDaysAreWorseThanOne()
        {
            Requisition twoDaysRequisition = new (new[]
            {
                new RequisitionItem(OopLab, "ФИИТ-101", "пн 1-6 пара", OopTeacher1),
                new RequisitionItem(OsLecture, "ФИИТ-101", "вт 4-6 пара", OsTeacher)            
            });
            var schedule = new Schedule(twoDaysRequisition, ClassRooms);

            var prevScore = 0d;
            while (schedule.NotUsedMeetings.Count != 0)
            {
                var meeting = schedule.GetMeetingsToAdd().First();
                schedule.AddMeeting(meeting);
                var curScore = estimator.Estimate(schedule);
                Assert.Greater(prevScore, curScore);
                prevScore = curScore;
            }
        }
    }
}