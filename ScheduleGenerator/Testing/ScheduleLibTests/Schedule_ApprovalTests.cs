using System;
using System.Collections.Generic;
using System.Linq;
using ApprovalTests;
using ApprovalTests.Reporters;
using Domain;
using Infrastructure;
using NUnit.Framework;
using ScheduleCLI;
using static Infrastructure.SheetConstants;

namespace Testing.ScheduleLibTests
{
    [TestFixture]
    [UseReporter(typeof(DiffReporter))]
    public class Schedule_ApprovalTests
    {
        [Test]
        public void CheckMeetingsPlacedSpring_Approval()
        {
            var unifiedString = GenerateApprovalString(SpringConfig);
            Approvals.Verify(unifiedString);
        }
        
        [Test]
        public void CheckMeetingsPlacedAutumn_Approval()
        {
            var unifiedString = GenerateApprovalString(AutumnConfig);
            Approvals.Verify(unifiedString);
        }

        private static string GenerateApprovalString(SheetNamesConfig config)
        {
            var solver = Program.GetSolver(config, Repository);
            var schedule = solver
                .GetSolution(new(0, 0, 0, 0, 1))
                .Schedule;

            var placedMeetings = GetOrderedMeetings(schedule.Meetings);
            var notePlacedMeetings = GetOrderedMeetings(schedule.NotUsedMeetings);
            var unifiedString =
                $"Placed:{placedMeetings.Count} Left:{schedule.NotUsedMeetings.Count}{Environment.NewLine}Placed:{Environment.NewLine}" +
                $"{string.Join(Environment.NewLine, placedMeetings)}{Environment.NewLine}" +
                $"Not placed:{Environment.NewLine}" +
                $"{string.Join(Environment.NewLine, notePlacedMeetings)}{Environment.NewLine}";
            return unifiedString;
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