using System;
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

        [TestCase(typeof(MeetingsPerDayEstimator))]
        [TestCase(typeof(StudentsSpacesEstimator))]
        [TestCase(typeof(TeacherPriorityEstimator))]
        [TestCase(typeof(TeacherSpacesEstimator))]
        [TestCase(typeof(TeacherUsedDaysEstimator))]
        [TestCase(typeof(GroupPriorityEstimator))]
        public static void EstimatorSameDeltaTest(Type estimatorType)
        {
            var estimator = Activator.CreateInstance(estimatorType) as IEstimator;
            AssertSameScoreDelta(estimator!);
        }

        [Test]
        public static void CombinedEstimatorSameDeltaTest()
        {
            var estimator = GetDefaultCombinedEstimator();
            AssertSameScoreDelta(estimator);
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
                Assert.AreEqual(scheduleScoreDelta, meetingScoreDelta, 1d / 1024);
                previousScheduleScore = currentScheduleScore;
            }
        }
    }
}