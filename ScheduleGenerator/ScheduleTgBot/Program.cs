﻿using System;
using System.Collections.Generic;
using System.Linq;
using Application.TelegramBot;
using System.IO;
using System.Xml.Linq;
using Ninject;
using Ninject.Extensions.Conventions;

namespace Application
{
    internal static class Program
    {
        private static readonly string DefaultConfigPath = Path.Combine("..", "..", "..", "App.config");

        private static void Main(string[] args)
        {
            var configPath = DefaultConfigPath;
            if (args.Length > 0) configPath = args[0];

            var doc = XDocument.Load(configPath);
            var configElement = doc.Element("configuration")!;
            var googleSheetConf = configElement.Element("googleSheetsApi")!;
            var credentialsEnvVar = googleSheetConf.Element("credentialsEnvVar")!.Attribute("val")!.Value;
            var credentialsFileName = googleSheetConf.Element("credentialsFileName")!.Attribute("val")!.Value;
            var secretTokenEnvVar = configElement
                .Element("telegramApi")!
                .Element("secretTokenEnvVar")!
                .Attribute("val")!.Value;
            var firebaseApiConf = configElement.Element("firebaseApi")!;
            var secretFirebaseTokenVar = firebaseApiConf.Element("secretTokenVar")!.Attribute("val")!.Value;
            var basePath = firebaseApiConf.Element("basePath")!.Attribute("val")!.Value;

            var requisitionSheetHeaders = new List<string>();
            var requirementsSheetHeaderComments = new List<string>();

            var learningPlanSheetHeaders = new List<string>();
            var learningPlanSheetHeaderComments = new List<string>();

            var requisitionPatternMsgList = new List<(string pattern, string msg)>();
            var learningPlanPatternMsgList = new List<(string pattern, string msg)>();

            var requisitionSheetSettings = configElement
                .Element("sheetSettings")!
                .Elements("sheet").First(e => e.Attribute("name")!.Value == "Requisitions");
            FillHeaderConfiguration(requisitionSheetSettings, requisitionSheetHeaders,
                requirementsSheetHeaderComments, requisitionPatternMsgList);

            var learningPlanSheetSettings = configElement
                .Element("sheetSettings")!
                .Elements("sheet").First(e => e.Attribute("name")!.Value == "LearningPlan");
            FillHeaderConfiguration(learningPlanSheetSettings, learningPlanSheetHeaders,
                learningPlanSheetHeaderComments, learningPlanPatternMsgList);


            var credentialDirPath = Environment.GetEnvironmentVariable(credentialsEnvVar)!;
            var credentialPath = Path.Combine(credentialDirPath, credentialsFileName);

            var secretToken = Environment.GetEnvironmentVariable(secretTokenEnvVar)!;

            var firebaseSecret = Environment.GetEnvironmentVariable(secretFirebaseTokenVar)!;

            var container = ConfigureContainer();

            Console.WriteLine("Starting...");

            var bot = new TgBot(secretToken, credentialPath, firebaseSecret, basePath,
                requisitionSheetHeaders, requirementsSheetHeaderComments,
                learningPlanSheetHeaders, learningPlanSheetHeaderComments,
                requisitionPatternMsgList, learningPlanPatternMsgList, container);
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
                var name = header.Attribute("name")!.Value;
                headers.Add(name);
                var description = header.Element("description")!.Value;
                comments.Add(description);
                var pattern = header.Element("pattern")!.Attribute("value")!.Value;
                var errorMessage = header.Element("errorMessage")!.Attribute("value")!.Value;
                patternMsg.Add((pattern, errorMessage));
            }
        }

        private static StandardKernel ConfigureContainer()
        {
            var container = new StandardKernel();
            container.Bind(c => c.FromThisAssembly().SelectAllClasses().BindAllInterfaces());
            return container;
        }
    }
}