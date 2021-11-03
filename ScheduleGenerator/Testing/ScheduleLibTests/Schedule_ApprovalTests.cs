using System;
using System.Linq;
using ApprovalTests;
using ApprovalTests.Reporters;
using NUnit.Framework;
using static Infrastructure.SheetConstants;
using static Domain.DomainExtensions;

namespace Testing.ScheduleLibTests
{
    [TestFixture]
    [Explicit]
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

            var placedMeetings = schedule
                .Meetings
                .Select(m => $"{m.Discipline} {m.MeetingType} {m.Teacher}")
                .OrderBy(m => m)
                .ToList();
            var notePlacedMeetings = schedule
                .NotUsedMeetings
                .Select(m => $"{m.Discipline} {m.MeetingType} {m.Teacher}")
                .OrderBy(m => m)
                .ToList();
            var unifiedString =
                $"Placed:{placedMeetings.Count} Left:{schedule.NotUsedMeetings.Count}, Placed:{Environment.NewLine}" +
                $"{string.Join(Environment.NewLine, placedMeetings)}{Environment.NewLine}" +
                $"Not placed:{Environment.NewLine}" +
                $"{string.Join(Environment.NewLine, notePlacedMeetings)}{Environment.NewLine}";
            Approvals.Verify(unifiedString);
        }
    }
}