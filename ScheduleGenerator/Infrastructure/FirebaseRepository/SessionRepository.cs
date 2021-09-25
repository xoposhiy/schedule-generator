using System;
using Firebase.Database;
using Firebase.Database.Query;
using System.Threading.Tasks;

namespace Infrastructure.FirebaseRepository
{
    public class ScheduleSession
    {
        public long Id;
        public string? SpreadsheetUrl;
        public string? InputRequirementsSheet;
        public string? ScheduleSheet;
        public string? LearningPlanSheet;
        public DateTime? LastModificationTime;
        public string? LastModificationInitiator;
        public DialogState DialogState;

        public override string ToString()
        {
            return string.Format(@"Id: {0}
SpreadsheetUrl: {1}
InputRequirementsSheet: {2}
LearningPlanSheet: {3}
ScheduleSheet: {4}
LastModificationTime: {5}
LastModificationInitiator: {6}
DialogState: {7}", Id, SpreadsheetUrl, InputRequirementsSheet, LearningPlanSheet, ScheduleSheet,
    LastModificationTime, LastModificationInitiator, DialogState);
        }
    }

    public enum DialogState
    {
        Initial,
        WaitSpreadsheetChangeConfirmation,
    }

    public interface IDBRepository
    {
        ScheduleSession Get(long telegramChatId);
        void Save(long telegramChatId, ScheduleSession session);
        void Delete(long telegramChatId);
        
    }

    public class SessionRepository : IDBRepository
    {
        private string sessionsKey = "scheduleSessions";
        private FirebaseClient dbClient;
        public SessionRepository(string basePath, string authSecret)
        {
            dbClient = new FirebaseClient(
              basePath,
              new FirebaseOptions
              {
                  AuthTokenAsyncFactory = () => Task.FromResult(authSecret)
              });
        }

        public ScheduleSession Get(long telegramChatId)
        {
            var sessionTask = dbClient
                .Child(sessionsKey)
                .Child(telegramChatId.ToString())
                .OnceSingleAsync<ScheduleSession>();
            sessionTask.Wait();
            var session = sessionTask.Result;
            return session;
        }

        public void Save(long telegramChatId, ScheduleSession session)
        {
            Console.WriteLine("<Saving>");
            var savingTask = dbClient
              .Child(sessionsKey)
              .Child(telegramChatId.ToString())
              .PutAsync(session);
            Console.WriteLine("Waiting Task ...");
            savingTask.Wait();
            Console.WriteLine("Saved on {0}/{1}", sessionsKey, telegramChatId);
        }

        public async void SaveAsync(long telegramChatId, ScheduleSession session)
        {
            await dbClient
              .Child(sessionsKey)
              .Child(telegramChatId.ToString())
              .PutAsync(session);
        }

        public async void Delete(long telegramChatId)
        {
            await dbClient
              .Child(sessionsKey)
              .Child(telegramChatId.ToString())
              .DeleteAsync();
        }
    }
}
