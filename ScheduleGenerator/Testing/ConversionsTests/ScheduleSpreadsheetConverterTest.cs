using System;
using System.Linq;
using Domain.Conversions;
using Domain.ScheduleLib;
using Infrastructure.GoogleSheetsRepository;
using NUnit.Framework;

namespace Testing.ConversionsTests
{
    public class Tests
    {
        private static string CredentialsEnvVar = "GoogleApiCredentials";
        private static readonly string ApplicationName = "MyApp";

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var testSchedule = new Schedule(new[]
            {
                MeetingCreator.CreateMeeting("Math 623 Fil 0 3 0 0 FT-202#0 FT-201#0 KN-201#2"),
                MeetingCreator.CreateMeeting("DM 622 Str 0 0 0 0 FT-202#0 KN-201#0"),
                MeetingCreator.CreateMeeting("OOP 526 Eg 0 2 0 0 FT-202#1 FT-202#0 FT-201#1 FT-201#2"),
                MeetingCreator.CreateMeeting("Net 150 Ber 0 1 1 0 FT-202#0"),
                MeetingCreator.CreateMeeting("Net 150 Ber 0 1 0 0 FT-201#1")
            });
            var credentialDirPath = Environment.GetEnvironmentVariable(CredentialsEnvVar);
            var credentialPath = credentialDirPath + "\\client_secrets.json";
            var url = "https://docs.google.com/spreadsheets/d/1JxL_CTuc-NLaBRdY5F4vz3yn6WJe8bp-7Mn7ViycjtQ/edit#gid=0";
            var repo = new GsRepository(ApplicationName, credentialPath, url);
            var converter = new ScheduleSpreadsheetConverter(repo, repo.CurrentSheetInfo!.Sheets.Keys.First());

            converter.Build(testSchedule);

            Assert.Pass();
        }
    }
}