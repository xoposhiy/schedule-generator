using System;
using System.Linq;
using NUnit.Framework;
using ApprovalTests;
using ApprovalTests.Reporters;
using Domain;
using Domain.Algorithms;
using Domain.Conversions;
using Infrastructure.GoogleSheetsRepository;

namespace Testing.ScheduleLibTests
{
    [TestFixture]
    [UseReporter(typeof(DiffReporter))]
    public class Schedule_ApprovalTests
    {
        [Test]
        public void CheckMeetingsPlaced_Approval()
        {
            // TODO сейчас это копипаста мейна - пофиксить дублирование

            var credentialPath = "..\\..\\..\\..\\Credentials\\client_secrets.json";

            var link =
                "https://docs.google.com/spreadsheets/d/1Q9imoj8xLFgp887NsYeW8ngJ53E5GHvKblrnfatEBHk/edit#gid=";
            var inputRequirementsSheetId = 861045221;
            var inputRequirementsSheetUrl = link + inputRequirementsSheetId;
            var repo = new GsRepository("test", credentialPath, inputRequirementsSheetUrl);
            repo.SetUpSheetInfo();
            var inputRequirementsSheetName = "Входные требования";
            var learningPlanSheetName = "Учебный план";
            //var scheduleSheetName = "Расписание";
            var classroomsSheetName = "Аудитории";

            var (requisitions, _, classrooms) = SheetToRequisitionConverter.ConvertToRequisitions(
                repo, inputRequirementsSheetName, learningPlanSheetName, classroomsSheetName);

            var requisition = new Requisition(requisitions.ToArray());

            var estimator = ScheduleCLI.Program.GetDefaultCombinedEstimator();

            var solver = new GreedySolver(estimator, requisition, classrooms, new(42));
            var schedule = solver
                .GetSolution(new(0, 1, 5))
                .Last()
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