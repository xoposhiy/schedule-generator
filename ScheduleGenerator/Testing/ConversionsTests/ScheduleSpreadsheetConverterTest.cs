using System.Linq;
using Domain;
using Domain.Conversions;
using Infrastructure.GoogleSheetsRepository;
using NUnit.Framework;
using static Testing.ObjectMother;

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
            var testSchedule = new Schedule(FullMondayRequisition, ClassRooms);

            while (testSchedule.NotUsedMeetings.Count != 0)
            {
                var meeting = testSchedule.GetMeetingsToAdd().First();
                testSchedule.AddMeeting(meeting, true);
            }

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