using System;
using System.Linq;
using Domain;
using Domain.Algorithms.Estimators.GroupsEstimators;
using Domain.MeetingsParts;
using NUnit.Framework;
using static Testing.ObjectMother;

namespace Testing.EstimatorsTests
{
    [TestFixture]
    public class MeetingsPerDayEstimatorTests
    {
        private readonly MeetingsPerDayEstimator estimator = new();

        private static void AssertBetweenZeroAndNegativeOne(double score)
        {
            Assert.GreaterOrEqual(0, score);
            Assert.GreaterOrEqual(score, -1);
        }

        [Test]
        public void SingleMeetingSingleDayTest()
        {
            var schedule = new Schedule(OsRequisition, ClassRooms);
            var osLecture = schedule.GetMeetingsToAdd().First();
            var meetingScore = estimator.EstimateMeetingToAdd(schedule, osLecture);
            AssertBetweenZeroAndNegativeOne(meetingScore);
            
            schedule.AddMeeting(osLecture);
            var scheduleScore = estimator.Estimate(schedule);
            AssertBetweenZeroAndNegativeOne(scheduleScore);
        }

        [Test]
        public void FewMeetingsSingleDayTest()
        {
            var schedule = new Schedule(CalculusRequisition, ClassRooms);
            while (schedule.NotUsedMeetings.Count != 0)
            {
                var scheduleScore = estimator.Estimate(schedule);
                AssertBetweenZeroAndNegativeOne(scheduleScore);

                var meeting = schedule.GetMeetingsToAdd().First();
                var meetingScore = estimator.EstimateMeetingToAdd(schedule, meeting);
                Assert.LessOrEqual(Math.Abs(meetingScore), 1);
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
            Assert.Negative(finalScore);
        }

        [Test]
        public void TwoBadDaysAreWorseThanOne()
        {
            Requisition twoDaysRequisition = new(new[]
            {
                new RequisitionItem(OopLab, "ФИИТ-101", "пн: 1-6 пара", OopTeacher1),
                new RequisitionItem(OsLecture, "ФИИТ-101", "вт: 4-6 пара", OsTeacher)
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