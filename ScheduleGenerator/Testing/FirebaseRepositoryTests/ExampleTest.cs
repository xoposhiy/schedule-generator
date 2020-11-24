using NUnit.Framework;
using System;
using FirebaseRepository;

namespace SessionRepositoryTests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var basePath = "https://schedule-generator-5f50e.firebaseio.com/";
            var authSecret = Environment.GetEnvironmentVariable("FirebaseSecret");

            var repo = new SessionRepository(basePath, authSecret);

            ulong sessionId = 27;
            var newSession = new ScheduleSession()
            {
                Id = sessionId,
                SpreadsheetUrl = "some url34567689",
                ScheduleSheet = "forSchedule2",
                InputRequirementsSheet = "inputSheet2",
                RoomsSheet = "rooms2",
                DialogState = DialogState.WaitSpreadsheetChangeConfirmation,
                LastModificationTime = DateTime.Now,
                LastModificationInitiator = "RTDBRepository"
            };

            repo.Save(sessionId, newSession);
            var session = repo.Get(sessionId);

            Assert.AreEqual(newSession.Id, session.Id);
            Assert.AreEqual(newSession.SpreadsheetUrl, session.SpreadsheetUrl);
            Assert.AreEqual(newSession.ScheduleSheet, session.ScheduleSheet);
            Assert.AreEqual(newSession.InputRequirementsSheet, session.InputRequirementsSheet);
            Assert.AreEqual(newSession.RoomsSheet, session.RoomsSheet);
            Assert.AreEqual(newSession.DialogState, session.DialogState);
            Assert.AreEqual(newSession.LastModificationInitiator, session.LastModificationInitiator);
        }
    }
}