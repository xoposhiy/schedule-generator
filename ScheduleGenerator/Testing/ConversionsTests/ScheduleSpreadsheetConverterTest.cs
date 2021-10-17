using Domain;
using Domain.Conversions;
using Infrastructure.GoogleSheetsRepository;
using NUnit.Framework;

namespace Testing.ConversionsTests
{
    [TestFixture]
    public class ScheduleSpreadSheetConverterTests
    {
        private const string Url =
            "https://docs.google.com/spreadsheets/d/1Q9imoj8xLFgp887NsYeW8ngJ53E5GHvKblrnfatEBHk/edit#gid=";

        private const string SheetName = "ScheduleTesting";

        [Test]
        public void ScheduleWriteTest()
        {
            var testSchedule = new Schedule(new[]
            {
                MeetingCreator.CreateMeeting("Math 623 Fil 0 3 0 0 FT-202#0 FT-201#0 KN-201#2"),
                MeetingCreator.CreateMeeting("DM 622 Str 0 0 0 0 FT-202#0 KN-201#0"),
                MeetingCreator.CreateMeeting("OOP 526 Eg 0 2 0 0 FT-202#1 FT-202#0 FT-201#1 FT-201#2"),
                MeetingCreator.CreateMeeting("Net 150 Ber 0 1 1 0 FT-202#0"),
                MeetingCreator.CreateMeeting("Net 150 Ber 0 1 0 0 FT-201#1")
            });
            const string credentialPath = "..\\..\\..\\..\\Credentials\\client_secrets.json";
            var repo = new GsRepository("test", credentialPath, Url);
            repo.ClearCellRange(SheetName, (0, 0), (100, 100));
            var converter = new ScheduleSpreadsheetConverter(repo, SheetName);

            converter.Build(testSchedule);

            //TODO: проверок бы добавить
            Assert.Pass();
        }
    }
}