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
            return $@"Id: {Id}
SpreadsheetUrl: {SpreadsheetUrl}
InputRequirementsSheet: {InputRequirementsSheet}
LearningPlanSheet: {LearningPlanSheet}
ScheduleSheet: {ScheduleSheet}
LastModificationTime: {LastModificationTime}
LastModificationInitiator: {LastModificationInitiator}
DialogState: {DialogState}";
        }
    }

    public enum DialogState
    {
        Initial,
        WaitSpreadsheetChangeConfirmation,
    }

    public interface IDbRepository
    {
        ScheduleSession Get(long telegramChatId);
        void Save(long telegramChatId, ScheduleSession session);
        void Delete(long telegramChatId);
        
    }

    public class SessionRepository : IDbRepository
    {
        private const string SessionsKey = "scheduleSessions";
        private readonly FirebaseClient dbClient;
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
                .Child(SessionsKey)
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
              .Child(SessionsKey)
              .Child(telegramChatId.ToString())
              .PutAsync(session);
            Console.WriteLine("Waiting Task ...");
            savingTask.Wait();
            Console.WriteLine("Saved on {0}/{1}", SessionsKey, telegramChatId);
        }

        public async void SaveAsync(long telegramChatId, ScheduleSession session)
        {
            await dbClient
              .Child(SessionsKey)
              .Child(telegramChatId.ToString())
              .PutAsync(session);
        }

        public async void Delete(long telegramChatId)
        {
            await dbClient
              .Child(SessionsKey)
              .Child(telegramChatId.ToString())
              .DeleteAsync();
        }
    }
}
