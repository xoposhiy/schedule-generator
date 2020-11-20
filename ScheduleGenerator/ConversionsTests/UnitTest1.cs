using NUnit.Framework;
using System;
using System.Linq;
using System.Collections.Generic;


using Conversions;
using GoogleSheetsRepository;
using ScheduleLib;

namespace ConversionsTests
{
    public class Tests
    {
        private static string CredentialsEnvVar = "GoogleApiCredentials";
        static readonly string ApplicationName = "MyApp";
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var credentialDirPath = Environment.GetEnvironmentVariable(CredentialsEnvVar);
            var credentialPath = credentialDirPath + "\\client_secrets.json";
            var url = "https://docs.google.com/spreadsheets/d/1JxL_CTuc-NLaBRdY5F4vz3yn6WJe8bp-7Mn7ViycjtQ/edit#gid=0";
            var repo = new GSRepository(ApplicationName, credentialPath, url);
            var converter = new ScheduleSpreadsheetConverter(repo, repo.CurrentSheetInfo.Sheets.Keys.First());
            repo.ModifySpreadSheet(repo.CurrentSheetInfo.Sheets.Keys.First())
                .ClearAll()
                .UnMergeAll()
                .Execute();
            FillSheetWithSchedule(converter);
            repo.ModifySpreadSheet(repo.CurrentSheetInfo.Sheets.Keys.First())
                .ClearAll()
                .UnMergeAll()
                .Execute();
            Assert.Pass();
        }

        private void FillSheetWithSchedule(ScheduleSpreadsheetConverter converter) {
            converter.Build(new Schedule(new[] {
                CreateMeeting("Math 623 Fil 0 3 0 FT-202#0 FT-201#0 KN-201#2"),
                CreateMeeting("DM 622 Str 0 0 0 FT-202#0 KN-201#0"),
                CreateMeeting("OOP 526 Eg 0 2 0 FT-202#1 FT-202#0 FT-201#1 FT-201#2"),
                CreateMeeting("Net 150 Ber 0 1 1 FT-202#0"),
                CreateMeeting("Net 150 Ber 0 1 0 FT-201#1"),
            }));
        }

        private static Meeting CreateMeeting(string entry)
        {
            var parts = entry.Split();
            var discipline = parts[0];
            var location = parts[1];
            var teacher = new Teacher(parts[2]);
            var dayOfWeek = (DayOfWeek)((int.Parse(parts[3]) + 1) % 7);
            var slotIndex = int.Parse(parts[4]);
            var weekType = (WeekType)int.Parse(parts[5]);
            var groups = new List<MeetingGroup>();
            foreach (var e in parts.Skip(6))
            {
                var namePart = e.Split('#');
                groups.Add(new MeetingGroup(namePart[0], (GroupPart)int.Parse(namePart[1])));
            }
            var meeting = new Meeting(new Discipline(discipline), MeetingType.Seminar, groups.ToArray());
            meeting.Location = location;
            meeting.Teacher = teacher;
            meeting.MeetingTime = new MeetingTime(dayOfWeek, slotIndex);
            meeting.WeekType = weekType;
            return meeting;
        }
    }
}