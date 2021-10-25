using System;
using System.Linq;
using Domain.Algorithms;
using Domain.Algorithms.Estimators;
using Domain.Conversions;
using Domain.MeetingsParts;
using Infrastructure;
using Infrastructure.GoogleSheetsRepository;
using Ninject;
using Ninject.Extensions.Conventions;
using static Infrastructure.SheetConstants;

namespace ScheduleCLI
{
    public static class Program
    {
        private static void Main()
        {
            //var container = ConfigureContainer();

            Console.WriteLine("Starting...");

            var inputRequirementsSheetId = 861045221;
            var inputRequirementsSheetUrl = Url + inputRequirementsSheetId;
            var repo = new GsRepository("test", CredentialPath, inputRequirementsSheetUrl);
            repo.SetUpSheetInfo();

            var (requisitions, _, classrooms) = SheetToRequisitionConverter.ConvertToRequisitions(
                repo, InputRequirementsSheetName, LearningPlanSheetName, ClassroomsSheetName);

            var requisition = new Requisition(requisitions.ToArray());

            // TODO все Estimators: нормализовать score во всех estimator-ах, чтобы масштаб чисел на выходе был схожий.
            // TODO вынести подготовительные шаги в отдельные методы (и пофиксить дублирование в тестах)

            var estimator = GetDefaultCombinedEstimator();

            var solver = new GreedySolver(estimator, requisition, classrooms, new(42));
            var solutions = solver.GetSolution(new(0, 1, 5)).ToList();

            var converter = new ScheduleSpreadsheetConverter(repo, ScheduleSheetName);
            converter.Build(solutions.Last().Schedule);
            var logger = new Logger("Combined");
            estimator.Estimate(solutions.Last().Schedule, logger);
            Console.WriteLine(logger);
            // Console.WriteLine(solutions.Last().Score);
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
            var teacherUsedDaysEstimator = (new TeacherUsedDaysEstimator(), 1);
            var priorityEstimator = (new PriorityMeetingsEstimator(), 100500);
            var estimator = new CombinedEstimator(basic, groupsSpacesEstimator,
                meetingsPerDayEstimator, teacherSpacesEstimator, teacherUsedDaysEstimator, priorityEstimator);
            return estimator;
        }
    }
}