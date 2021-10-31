using System.Collections.Generic;
using System.Linq;
using ApprovalTests;
using ApprovalTests.Reporters;
using Domain.Conversions;
using Domain.MeetingsParts;
using Infrastructure.SheetPatterns;
using NUnit.Framework;
using static Infrastructure.SheetConstants;


namespace Testing.ConversionsTests
{
    [TestFixture]
    [UseReporter(typeof(DiffReporter))]
    public class Conversions_ApprovalTests
    {
        //TODO make actual test
        [Test]
        public void TimeRequisitionsReadTest()
        {
            var requisitionData = SheetTableReader
                .ReadRowsFromSheet(Repository, InputRequirementsTestSheetName, 1, 0, 7)
                .Take(10);
            //var allTimeRequisitions = new List<List<MeetingTimeRequisition>>();
            var checkStrings = new List<string>();
            foreach (var requisitionRow in requisitionData)
            {
                var meetingTimesRaw = requisitionRow[5];
                var meetingTimeRequisitions =
                    SheetToRequisitionConverter.ParseMeetingTimeRequisitions(meetingTimesRaw);
                var a = meetingTimeRequisitions.Select(r => string.Join('\n',r.MeetingTimeChoices.Select(m=>m.ToString())));
                var checkString = 
                    $"{meetingTimesRaw}\n{string.Join("\n\n", a)}";
                checkStrings.Add(checkString);
            }
            var unifiedString = string.Join("\n------\n", checkStrings);
            Approvals.Verify(unifiedString);
        }
    }
}