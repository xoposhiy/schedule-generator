using System;
using System.Linq;
using Domain.Algorithms;
using Domain.Conversions;
using Domain.Estimators;
using Domain.Rules;
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

            var credentialPath = "C:\\Users\\t.belov\\Desktop\\Git repos" +
                                 "\\schedule-generator\\ScheduleGenerator\\Credentials\\client_secrets.json";
            
            var container = ConfigureContainer();

            Console.WriteLine("Starting...");
            
            var link =
                "https://docs.google.com/spreadsheets/d/1Q9imoj8xLFgp887NsYeW8ngJ53E5GHvKblrnfatEBHk/edit#gid=";
            var inputRequirementsSheetId = 861045221;
            var learningPlanSheetId = 493250469;
            var scheduleSheetId = 0;
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
            var evaluator = container.Get<MeetingEvaluator>();

            var requisition = new Requisition(requisitions.ToArray());
            
            var basic = new BasicEstimator();
            var groupsSpacesEstimator = new GroupSpacesEstimator();
            var teacherSpacesEstimator = new TeacherSpacesEstimator();
            var meetingsPerDayEstimator = new MeetingsPerDayEstimator();
            var teacherUsedDaysEstimator = new TeacherUsedDaysEstimator();
            var estimator = new CombinedEstimator(groupsSpacesEstimator,
                meetingsPerDayEstimator,teacherSpacesEstimator, teacherUsedDaysEstimator);
            var solver = new GreedySolver(estimator, requisition, classrooms, new Random());
            var solutions = solver.GetSolution(new TimeSpan(0 ,1, 5)).ToList();
            
            var converter = new ScheduleSpreadsheetConverter(repo, scheduleSheetName);
            converter.Build(solutions.Last().Schedule);

        }
        

        private static StandardKernel ConfigureContainer() {
            var container = new StandardKernel();
            container.Bind(c => c.FromThisAssembly().SelectAllClasses().BindAllInterfaces());
            container.Bind<MeetingEvaluator>().ToSelf();
            container.Bind<IRule>().To<NoMoreThanOneMeetingAtTimeForGroupRule>();
            // container.Bind<IRule>().To<NoMoreThanOneMeetingAtTimeForTeacherRule>();

            return container;
        }
    }
}