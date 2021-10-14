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
            var requisition = new Requisition(new[]
            {
                new RequisitionItem(CalculusLecture, "ФИИТ-101", "пн 1-2 пара", CalculusTeacher),
                new RequisitionItem(CalculusSeminar, "ФИИТ-101", "пн 3 пара", CalculusTeacher),
                new RequisitionItem(OsLecture, "ФИИТ-101", "пн 4 пара", OsTeacher),
                new RequisitionItem(OopLab, "ФИИТ-101", "пн 5 пара", OopTeacher2),
                new RequisitionItem(OopSeminar, "ФИИТ-101", "пн 6 пара", OopTeacher2),
            });
            
            var schedule = new Schedule(requisition, ClassRooms);
            
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