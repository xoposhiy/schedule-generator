using System;
using System.Linq;
using Domain;
using Domain.Algorithms;
using Domain.Algorithms.Estimators;
using Domain.Conversions;
using Infrastructure.GoogleSheetsRepository;
using Ninject;
using Ninject.Extensions.Conventions;

namespace ScheduleCLI
{
    public static class Program
    {
        private static void Main()
        {
            var credentialPath = "..\\..\\..\\..\\Credentials\\client_secrets.json";

            //var container = ConfigureContainer();

            Console.WriteLine("Starting...");

            var link =
                "https://docs.google.com/spreadsheets/d/1Q9imoj8xLFgp887NsYeW8ngJ53E5GHvKblrnfatEBHk/edit#gid=";
            var inputRequirementsSheetId = 861045221;
            var inputRequirementsSheetUrl = link + inputRequirementsSheetId;
            var repo = new GsRepository("test", credentialPath, inputRequirementsSheetUrl);
            repo.SetUpSheetInfo();
            var inputRequirementsSheetName = "Входные требования";
            var learningPlanSheetName = "Учебный план";
            var scheduleSheetName = "Расписание";
            var classroomsSheetName = "Аудитории";

            var (requisitions, _, classrooms) = SheetToRequisitionConverter.ConvertToRequisitions(
                repo, inputRequirementsSheetName, learningPlanSheetName, classroomsSheetName);
            // foreach (var requisitionItem in requisitions)
            // {
            //     Console.WriteLine(requisitionItem.ToString());
            // }
            //var evaluator = container.Get<MeetingEvaluator>();

            var requisition = new Requisition(requisitions.ToArray());

            // TODO все Estimators: нормализовать score во всех estimator-ах, чтобы масштаб чисел на выходе был схожий.
            // TODO вынести подготовительные шаги в отдельные методы (и пофиксить дублирование в тестах)

            var estimator = GetDefaultCombinedEstimator();

            var solver = new GreedySolver(estimator, requisition, classrooms, new(42));
            var solutions = solver.GetSolution(new(0, 1, 5)).ToList();

            var converter = new ScheduleSpreadsheetConverter(repo, scheduleSheetName);
            converter.Build(solutions.Last().Schedule);
        }

        // ReSharper disable once UnusedMember.Local
        private static StandardKernel ConfigureContainer()
        {
            var container = new StandardKernel();
            container.Bind(c => c.FromThisAssembly().SelectAllClasses().BindAllInterfaces());
            return container;
        }

        public static CombinedEstimator GetDefaultCombinedEstimator()
        {
            var basic = (new FreedomDegreeEstimator(), 100);
            var groupsSpacesEstimator = (new StudentsSpacesEstimator(), 1);
            var teacherSpacesEstimator = (new TeacherSpacesEstimator(), 1);
            var meetingsPerDayEstimator = (new MeetingsPerDayEstimator(), 1);
            var teacherUsedDaysEstimator = (new TeacherUsedDaysEstimator(), 10);
            var estimator = new CombinedEstimator(basic, groupsSpacesEstimator,
                meetingsPerDayEstimator, teacherSpacesEstimator, teacherUsedDaysEstimator);
            return estimator;
        }
    }
}