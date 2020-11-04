using System;
using Firebase.Database;
using Firebase.Database.Query;
using System.Threading.Tasks;
using System.Text;

namespace FirebaseRepository
{
    public class ScheduleSession
    {
        public ulong Id;
        public string? SpreadsheetUrl;
        public string? InputRequirementsSheet;
        public string? ScheduleSheet;
        public string? RoomsSheet;
        public DateTime? LastModificationTime;
        public string? LastModificationInitiator;
        public DialogState DialogState;

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append(String.Format("Id: {0}\n", Id));
            builder.Append(String.Format("SpreadsheetUrl: {0}\n", SpreadsheetUrl));
            builder.Append(String.Format("InputRequirementsSheet: {0}\n", InputRequirementsSheet));
            builder.Append(String.Format("RoomsSheet: {0}\n", RoomsSheet));
            builder.Append(String.Format("ScheduleSheet: {0}\n", ScheduleSheet));
            builder.Append(String.Format("LastModificationTime: {0}\n", LastModificationTime));
            builder.Append(String.Format("LastModificationInitiator: {0}\n", LastModificationInitiator));
            builder.Append(String.Format("DialogState: {0}\n", DialogState));
            return builder.ToString();
        }
    }

    public enum DialogState
    {
        Initial,
        WaitSpreadsheetChangeConfirmation,
    }

    public interface IDBRepository
    {
        ScheduleSession Get(ulong telegramChatId);
        void Save(ulong telegramChatId, ScheduleSession session);
    }

    public class SessionRepository : IDBRepository
    {
        private string sessionsKey = "scheduleSessions";
        private string secretsEnvVar = "FirebaseSecret";
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

        public SessionRepository()
        {
            var basePath = "https://schedule-generator-5f50e.firebaseio.com/";
            var authSecret = Environment.GetEnvironmentVariable(secretsEnvVar);
            if (authSecret is null)
                throw new Exception($"Env Variable with secret not found (check env var {secretsEnvVar})");
            dbClient = new FirebaseClient(
              basePath,
              new FirebaseOptions
              {
                  AuthTokenAsyncFactory = () => Task.FromResult(authSecret)
              });
        }

        public ScheduleSession Get(ulong telegramChatId)
        {
            var sessionTask = dbClient
                .Child(sessionsKey)
                .Child(telegramChatId.ToString())
                .OnceSingleAsync<ScheduleSession>();
            sessionTask.Wait();
            var session = sessionTask.Result;
            return session;
        }

        public void Save(ulong telegramChatId, ScheduleSession session)
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

        public async void SaveAsync(ulong telegramChatId, ScheduleSession session)
        {
            await dbClient
              .Child(sessionsKey)
              .Child(telegramChatId.ToString())
              .PutAsync(session);
        }
    }
}
