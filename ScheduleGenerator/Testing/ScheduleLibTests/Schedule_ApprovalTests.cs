﻿using System;
using System.Collections.Generic;
using System.Linq;
using ApprovalTests;
using ApprovalTests.Reporters;
using Domain;
using NUnit.Framework;
using static Infrastructure.SheetConstants;
using static Domain.DomainExtensions;

namespace Testing.ScheduleLibTests
{
    [TestFixture]
    [UseReporter(typeof(DiffReporter))]
    public class Schedule_ApprovalTests
    {
        [Test]
        public void CheckMeetingsPlaced_Approval()
        {
            var solver = GetSolver(SpringConfig, Repository);
            var schedule = solver
                .GetSolution(new(0, 0, 0, 0, 100))
                .Schedule;

            var placedMeetings = GetOrderedMeetings(schedule.Meetings);
            var notePlacedMeetings = GetOrderedMeetings(schedule.NotUsedMeetings);
            var unifiedString =
                $"Placed:{placedMeetings.Count} Left:{schedule.NotUsedMeetings.Count}, Placed:{Environment.NewLine}" +
                $"{string.Join(Environment.NewLine, placedMeetings)}{Environment.NewLine}" +
                $"Not placed:{Environment.NewLine}" +
                $"{string.Join(Environment.NewLine, notePlacedMeetings)}{Environment.NewLine}";
            Approvals.Verify(unifiedString);
        }

        private static List<string> GetOrderedMeetings(IEnumerable<Meeting> meetings)
        {
            return meetings
                .Select(m => $"{m.Discipline} {m.MeetingType} {m.Teacher}")
                .OrderBy(m => m)
                .ToList();
        }
    }
}