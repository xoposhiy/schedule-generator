﻿using System;
using System.Linq;
using Domain;
using Domain.Algorithms;
using Domain.Algorithms.Estimators;
using Domain.Algorithms.Estimators.GroupsEstimators;
using Domain.Algorithms.Estimators.PriorityEstimators;
using NUnit.Framework;
using static Domain.DomainExtensions;
using static Testing.ObjectMother;

namespace Testing.EstimatorsTests
{
    [TestFixture]
    public static class CombinedEstimatorTests
    {
        [TestCase(typeof(DayDurationEstimator))]
        [TestCase(typeof(LocationPerDayEstimator))]
        [TestCase(typeof(MeetingsPerDayEstimator))]
        [TestCase(typeof(StudentsSpacesEstimator))]
        [TestCase(typeof(TeacherSpacesEstimator))]
        [TestCase(typeof(TeacherUsedDaysEstimator))]
        [TestCase(typeof(GroupPriorityEstimator))]
        [TestCase(typeof(TimePriorityEstimator))]
        // [TestCase(typeof(LateMeetingsEstimator))]
        public static void EstimatorSameDeltaTest(Type estimatorType)
        {
            var estimator = Activator.CreateInstance(estimatorType) as IEstimator;
            AssertSameScoreDelta(estimator!);
        }

        [Test]
        [Explicit]
        public static void CombinedEstimatorSameDeltaTest()
        {
            var estimator = GetDefaultCombinedEstimator();
            AssertSameScoreDelta(estimator);
        }

        private static void AssertSameScoreDelta(IEstimator estimator)
        {
            var schedule = new Schedule(AutumnRequisition, ClassroomsRequisitions);
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