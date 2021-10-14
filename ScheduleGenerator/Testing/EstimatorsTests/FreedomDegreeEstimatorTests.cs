using System.Linq;
using Domain;
using Domain.Algorithms.Estimators;
using NUnit.Framework;
using static Testing.ScheduleLibTests.ObjectMother;


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
            Assert.AreNotEqual(0, score);
        }

        [Test]
        public void ZeroScoreWhenAllMeetingPlaced()
        {
            var schedule = new Schedule(OsRequisition, ClassRooms);
            var osLecture = schedule.GetMeetingsToAdd().First();
            schedule.AddMeeting(osLecture);
            var score = freedomDegreeEstimator.Estimate(schedule);
            Assert.AreEqual(0, score);
        }
    }
}