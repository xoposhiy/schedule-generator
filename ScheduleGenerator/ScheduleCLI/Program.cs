using System;
using System.Linq;
using Domain.Algorithms;
using Domain.Conversions;
using Domain.Estimators;
using Domain.ScheduleLib;
using Infrastructure.GoogleSheetsRepository;
using Ninject;
using Ninject.Extensions.Conventions;

namespace ScheduleCLI
{
    static class Program
    {
        static void Main(string[] args)
        {
            var credentialPath = "..\\..\\..\\..\\Credentials\\client_secrets.json";

            //var container = ConfigureContainer();

            Console.WriteLine("Starting...");

            var link =
                "https://docs.google.com/spreadsheets/d/1Q9imoj8xLFgp887NsYeW8ngJ53E5GHvKblrnfatEBHk/edit#gid=";
            var inputRequirementsSheetId = 861045221;
            var inputRequirementsSheetUrl = link + inputRequirementsSheetId;
            var repo = new GSRepository("test", credentialPath, inputRequirementsSheetUrl);
            repo.SetUpSheetInfo();
            var inputRequirementsSheetName = "Входные требования";
            var learningPlanSheetName = "Учебный план";
            var scheduleSheetName = "Расписание";
            var classroomsSheetName = "Аудитории";

            var (requisitions, learningPlan, classrooms) = SheetToRequisitionConverter.ConvertToRequisitions(
                repo, inputRequirementsSheetName, learningPlanSheetName, classroomsSheetName);
            // foreach (var requisitionItem in requisitions)
            // {
            //     Console.WriteLine(requisitionItem.ToString());
            // }
            //var evaluator = container.Get<MeetingEvaluator>();

            var requisition = new Requisition(requisitions.ToArray());


            // TODO все Estimtors: score -> penalty
            // TODO все Estimtors: нормализовать score во всех estimator-ах, чтобы масштаб чисел на выходе был схожий.

            var basic = (new FreedomDegreeEstimator(), 100);
            var groupsSpacesEstimator = (new StudentsSpacesEstimator(), 1);
            var teacherSpacesEstimator = (new TeacherSpacesEstimator(), 1);
            var meetingsPerDayEstimator = (new MeetingsPerDayEstimator(), 1);
            var teacherUsedDaysEstimator = (new TeacherUsedDaysEstimator(), 10);
            var estimator = new CombinedEstimator(basic, groupsSpacesEstimator,
                meetingsPerDayEstimator, teacherSpacesEstimator, teacherUsedDaysEstimator);
            var solver = new GreedySolver(estimator, requisition, classrooms, new Random());
            var solutions = solver.GetSolution(new TimeSpan(0, 1, 5)).ToList();

            var converter = new ScheduleSpreadsheetConverter(repo, scheduleSheetName);
            converter.Build(solutions.Last().Schedule);
        }


        private static StandardKernel ConfigureContainer()
        {
            var container = new StandardKernel();
            container.Bind(c => c.FromThisAssembly().SelectAllClasses().BindAllInterfaces());
            return container;
        }
    }
}