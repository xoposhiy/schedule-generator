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

namespace Application
{
 class Program
    {
        private static string defaultConfigPath = Path.Combine("..","..", "..", "App.config");

        static void Main(string[] args)
        {
            var configPath = defaultConfigPath;
            if (args.Length > 0) {
                configPath = args[0];
            }

            var doc = XDocument.Load(configPath);
            var configElement = doc.Element("configuration");
            var googleSheetConf = configElement.Element("googleSheetsApi");
            var credentialsEnvVar = googleSheetConf.Element("credentialsEnvVar").Attribute("val").Value;
            var credentialsFileName = googleSheetConf.Element("credentialsFileName").Attribute("val").Value;

            var firebaseApiConf = configElement.Element("firebaseApi");
            var secretFirebaseTokenVar = firebaseApiConf.Element("secretTokenVar").Attribute("val").Value;
            var basePath = firebaseApiConf.Element("basePath").Attribute("val").Value;

            var requisitionSheetHeaders = new List<string>();
            var requirmentsSheetHeaderComments = new List<string>();

            var learningPlanSheetHeaders = new List<string>();
            var learningPlanSheetHeaderComments = new List<string>();

            var requisitionPatternMsgList = new List<(string pattern, string msg)>();
            var learningPlanPatternMsgList = new List<(string pattern, string msg)>();

            var requisitionSheetSettings = configElement
                .Element("sheetSettings")
                .Elements("sheet").Where(e => e.Attribute("name").Value == "Requisitions").First();
            FillHeaderConfiguration(requisitionSheetSettings, requisitionSheetHeaders,
                requirmentsSheetHeaderComments, requisitionPatternMsgList);

            var learningPlanSheetSettings = configElement
                .Element("sheetSettings")
                .Elements("sheet").Where(e => e.Attribute("name").Value == "LearningPlan").First();
            FillHeaderConfiguration(learningPlanSheetSettings, learningPlanSheetHeaders,
                learningPlanSheetHeaderComments, learningPlanPatternMsgList);

            
            var credentialDirPath = Environment.GetEnvironmentVariable(credentialsEnvVar);
            var f = Environment.GetEnvironmentVariables();

            var credentialPath = Path.Combine(credentialDirPath, credentialsFileName);;

            
            var firebaseSecret = Environment.GetEnvironmentVariable(secretFirebaseTokenVar);

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
            var inputRequirementsSheetName = repo.CurrentSheetInfo.Sheets
                .Where(s => s.Value == inputRequirementsSheetId)
                .Select(s => s.Key)
                .First();
            var learningPlanSheetName = repo.CurrentSheetInfo.Sheets
                .Where(s => s.Value == learningPlanSheetId)
                .Select(s => s.Key)
                .First();
            var scheduleSheetName = repo.CurrentSheetInfo.Sheets
                .Where(s => s.Value == scheduleSheetId)
                .Select(s => s.Key)
                .First();
            
            var (requisitions, learningPlan) = SheetToRequisitionConverter.ConvertToRequisitions(
                repo, inputRequirementsSheetName, learningPlanSheetName);
            var evaluator = container.Get<MeetingEvaluator>();

            var requisition = new Requisition(requisitions.ToArray());

            var schedule = new GreedyScheduleGenerator().MakeSchedule(learningPlan, evaluator, requisition);

            var converter = new ScheduleSpreadsheetConverter(repo, scheduleSheetName);
            converter.Build(schedule);
        }

        private static void FillHeaderConfiguration(XElement baseElement,
                List<string> headers, List<string> comments,
                List<(string pattern, string msg)> patternMsg)
        {
            foreach (var header in baseElement.Elements("header"))
            {
                var name = header.Attribute("name").Value;
                headers.Add(name);
                var description = header.Element("description").Value;
                comments.Add(description);
                var pattern = header.Element("pattern").Attribute("value").Value;
                var errorMessage = header.Element("errorMessage").Attribute("value").Value;
                patternMsg.Add((pattern, errorMessage));
            }
        }

        private static StandardKernel ConfigureContainer() {
            var container = new StandardKernel();
            container.Bind(c => c.FromThisAssembly().SelectAllClasses().BindAllInterfaces());
            container.Bind<MeetingEvaluator>().ToSelf();

            return container;
        }
    }
}