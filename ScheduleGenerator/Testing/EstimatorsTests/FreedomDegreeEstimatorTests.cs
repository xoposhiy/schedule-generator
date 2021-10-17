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
            Assert.Less(0, score);
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
        
        [Test]
        public void ScoreDynamicWhenPlacingMeetings()
        {
            var schedule = new Schedule(FullMondayRequisition, ClassRooms);
            var score1 = freedomDegreeEstimator.Estimate(schedule);
            var meeting1 = schedule.GetMeetingsToAdd().Last();
            var meeting2 = schedule.NotUsedMeetings.Skip(2).First();
            var meeting2ScoreBefore = schedule.MeetingFreedomDegree[meeting2];
            schedule.AddMeeting(meeting1, true);
            var score2 = freedomDegreeEstimator.Estimate(schedule);
            var meeting2ScoreAfter = schedule.MeetingFreedomDegree[meeting2];
            Assert.Greater(score1, score2);
            // Assert.Greater(meeting2ScoreBefore, meeting2ScoreAfter);
            // TODO: truly recalculate FreedomDegree
        }
    }
}