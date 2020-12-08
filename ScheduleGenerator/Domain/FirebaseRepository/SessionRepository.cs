using System;
using Firebase.Database;
using Firebase.Database.Query;
using System.Threading.Tasks;
using System.Text;

namespace FirebaseRepository
{
    public class ScheduleSession
    {
        public long Id;
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
