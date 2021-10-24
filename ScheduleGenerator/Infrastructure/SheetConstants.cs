using Infrastructure.GoogleSheetsRepository;

namespace Infrastructure
{
    public static class SheetConstants
    {
        public const string Url =
            "https://docs.google.com/spreadsheets/d/1Q9imoj8xLFgp887NsYeW8ngJ53E5GHvKblrnfatEBHk/edit#gid=";

        public const string SheetName = "Testing";
        public const string CredentialPath = "..\\..\\..\\..\\Credentials\\client_secrets.json";

        public const string InputRequirementsSheetName = "Входные требования";
        public const string LearningPlanSheetName = "Учебный план";
        public const string ScheduleSheetName = "Расписание";
        public const string ClassroomsSheetName = "Аудитории";

        public static readonly GsRepository Repository = new("test", CredentialPath, Url);
    }
}