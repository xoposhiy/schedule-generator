using System.Linq;
using Domain;
using Domain.Algorithms.Estimators;
using NUnit.Framework;
using static Testing.ObjectMother;


namespace Testing.EstimatorsTests
{
    [TestFixture]
    public class FreedomDegreeEstimatorTests
    {
        private readonly FreedomDegreeEstimator freedomDegreeEstimator = new();

        [Test]
        public void NonZeroScoreWhenHasUnplacedMeeting()
        {
            var schedule = new Schedule(OsRequisition, ClassRooms);

            var score = freedomDegreeEstimator.Estimate(schedule);
            Assert.Positive(score);
        }

        [Test]
        public void ZeroScoreWhenAllMeetingPlaced()
        {
            var schedule = new Schedule(OsRequisition, ClassRooms);
            var osLecture = schedule.GetMeetingsToAdd().First();
            schedule.AddMeeting(osLecture);
            var score = freedomDegreeEstimator.Estimate(schedule);
            Assert.Zero(score);
        }

        [Explicit]
        [Test]
        public void ScoreDynamicWhenPlacingMeetings()
        {
            var schedule = new Schedule(FullMondayRequisition, ClassRooms);

            var freedomWhenEmpty = freedomDegreeEstimator.Estimate(schedule);

            var meetingToPlace = schedule.GetMeetingsToAdd().Last();
            var notPlacedMeeting = schedule.NotUsedMeetings.Skip(2).First();

            var notPlacedScoreBefore = schedule.MeetingFreedomDegree[notPlacedMeeting];

            schedule.AddMeeting(meetingToPlace, true);

            var freedomWhenSomePlaced = freedomDegreeEstimator.Estimate(schedule);
            var notPlacedScoreAfter = schedule.MeetingFreedomDegree[notPlacedMeeting];

            Assert.Greater(freedomWhenEmpty, freedomWhenSomePlaced);
            Assert.Greater(notPlacedScoreBefore, notPlacedScoreAfter);

            schedule.RemoveMeeting(meetingToPlace, true);

            Assert.AreEqual(notPlacedScoreBefore, schedule.MeetingFreedomDegree[notPlacedMeeting]);
        }
    }
}