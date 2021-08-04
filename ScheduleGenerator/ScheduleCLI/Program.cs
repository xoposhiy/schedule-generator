using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Domain.Algorithms;
using Domain.Conversions;
using Domain.Rules;
using Domain.ScheduleLib;
using Infrastructure.GoogleSheetsRepository;
using Ninject;
using Ninject.Extensions.Conventions;

namespace ScheduleCLI
{
    class Program
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
            
            var (requisitions, learningPlan) = SheetToRequisitionConverter.ConvertToRequisitions(
                repo, inputRequirementsSheetName, learningPlanSheetName);
            foreach (var requisitionItem in requisitions)
            {
                Console.WriteLine(requisitionItem.ToString());
            }
            var evaluator = container.Get<MeetingEvaluator>();

            var requisition = new Requisition(requisitions.ToArray());

            var schedule = new GreedyScheduleGenerator().MakeSchedule(learningPlan, evaluator, requisition);

            var converter = new ScheduleSpreadsheetConverter(repo, scheduleSheetName);
            converter.Build(schedule);
        }
        

        private static StandardKernel ConfigureContainer() {
            var container = new StandardKernel();
            container.Bind(c => c.FromThisAssembly().SelectAllClasses().BindAllInterfaces());
            container.Bind<MeetingEvaluator>().ToSelf();
            container.Bind<IRule>().To<NoMoreThanOneMeetingAtTimeForGroupRule>();

            return container;
        }
    }
}