using System.Collections.Generic;
using System.Linq;
using Domain;
using Domain.Algorithms;
using Domain.Algorithms.Estimators;
using Domain.Enums;
using Domain.MeetingsParts;
using NUnit.Framework;
using static Domain.DomainExtensions;
using static Infrastructure.SheetConstants;

namespace Testing.EstimatorsTests
{
    [TestFixture]
    public static class CombinedEstimatorTests
    {
        private static readonly Requisition Requisition;
        private static readonly Dictionary<string, List<RoomSpec>> ClassRooms;

        static CombinedEstimatorTests()
        {
            (Requisition, ClassRooms) = GetRequisition(AutumnConfig, Repository);
        }

        [Test]
        public static void EstimatorsSameDeltaTest()
        {
            IEstimator[] estimators =
            {
                new MeetingsPerDayEstimator()
                // new StudentsSpacesEstimator(),
                // new TeacherPriorityEstimator(),
                // new TeacherSpacesEstimator(),
                // new TeacherUsedDaysEstimator(),
                // GetDefaultCombinedEstimator(),
            };

            foreach (var estimator in estimators) AssertSameScoreDelta(estimator);
        }

        private static void AssertSameScoreDelta(IEstimator estimator)
        {
            var schedule = new Schedule(Requisition, ClassRooms);
            var previousScheduleScore = estimator.Estimate(schedule);

            while (true)
            {
                var meetingToAdd = schedule.GetMeetingsToAdd().FirstOrDefault();
                if (meetingToAdd == null) break;
                var meetingScoreDelta = estimator.EstimateMeetingToAdd(schedule, meetingToAdd);
                schedule.AddMeeting(meetingToAdd);
                var currentScheduleScore = estimator.Estimate(schedule);
                var scheduleScoreDelta = currentScheduleScore - previousScheduleScore;
                Assert.AreEqual(meetingScoreDelta, scheduleScoreDelta, 1d / 1024);
                previousScheduleScore = currentScheduleScore;
            }
        }
    }
}