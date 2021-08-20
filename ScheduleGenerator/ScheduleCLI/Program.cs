using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Domain.Algorithms;
using Domain.Conversions;
using Domain.Rules;
using Domain.ScheduleLib;
using Google.Apis.Sheets.v4.Data;
using Infrastructure.GoogleSheetsRepository;
using Ninject;
using Ninject.Extensions.Conventions;

namespace ScheduleCLI
{
    class Program
    {
        static async Task Main(string[] args)
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
            // var learningPlanSheetUrl = link + learningPlanSheetId;
            var repo = new GSRepository("test", credentialPath, inputRequirementsSheetUrl);
            repo.SetUpSheetInfo();
            // var inputRequirementsSheetName = repo.CurrentSheetInfo.Sheets
            //     .Where(s => s.Value == inputRequirementsSheetId)
            //     .Select(s => s.Key)
            //     .First();
            var inputRequirementsSheetName = "Входные требования";
            var learningPlanSheetName = "Учебный план";
            var scheduleSheetName = "Расписание";
            var classroomsSheetName = "Аудитории";
            
            var (requisitions, learningPlan, classrooms) = SheetToRequisitionConverter.ConvertToRequisitions(
                repo, inputRequirementsSheetName, learningPlanSheetName, classroomsSheetName);
            foreach (var requisitionItem in requisitions)
            {
                Console.WriteLine(requisitionItem.ToString());
            }
            var evaluator = container.Get<MeetingEvaluator>();

            var requisition = new Requisition(requisitions.ToArray());

            // var schedule = new GreedyScheduleGenerator().MakeSchedule(learningPlan, evaluator, requisition);
                
            // var converter = new ScheduleSpreadsheetConverter(repo, scheduleSheetName);
            // converter.Build(schedule);

            var schedule = new Schedule(requisition, classrooms);
            var s = Stopwatch.StartNew();
            while (true)
            {
                var f = schedule.GetMeetingsToAdd();
                var g = f.ToList();
                Console.WriteLine(g.Count);
                Console.WriteLine(schedule.NotUsedMeetings.Count);
                Console.WriteLine(schedule.Meetings.Count);
                Console.WriteLine();
                if (g.Count == 0)
                    break;
                foreach (var meeting in g.First())
                {
                    // if (meeting.MeetingType != MeetingType.Seminar)
                        schedule.AddMeeting(meeting);
                }
            }
            Console.WriteLine(s.Elapsed);
            
            var converter = new ScheduleSpreadsheetConverter(repo, scheduleSheetName);
            converter.Build(schedule);
            
            
            // var estimator = new BasicEstimator();
            // var solver = new GreedySolver(estimator, requisition, classrooms, new Random());
            // var solutions = solver.GetSolution(new TimeSpan(0 ,1, 5)).ToList();
            //
            // var converter = new ScheduleSpreadsheetConverter(repo, scheduleSheetName);
            // converter.Build(solutions.Last().Schedule);

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