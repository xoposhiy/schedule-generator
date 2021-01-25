using System;
using System.Collections.Generic;
using System.Linq;
using Application.TelegramBot;
using System.IO;
using System.Xml.Linq;


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
            var secretTokenEnvVar = configElement
                .Element("telegramApi")
                .Element("secretTokenEnvVar")
                .Attribute("val").Value;
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
            var credentialPath = Path.Combine(credentialDirPath, credentialsFileName);

            var secretToken = Environment.GetEnvironmentVariable(secretTokenEnvVar);

            var firebaseSecret = Environment.GetEnvironmentVariable(secretFirebaseTokenVar);


            Console.WriteLine("Starting...");

            var bot = new TBot(secretToken, credentialPath, firebaseSecret, basePath,
                requisitionSheetHeaders, requirmentsSheetHeaderComments,
                learningPlanSheetHeaders, learningPlanSheetHeaderComments,
                requisitionPatternMsgList, learningPlanPatternMsgList);
            bot.Start();
            Console.WriteLine("Press Enter to stop the Bot");
            Console.ReadLine();
            bot.Stop();
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
    }
}
