using Infrastructure.GoogleSheetsRepository;

namespace Infrastructure
{
    public record SheetNamesConfig(string TableUrl, string Requirements, string LearningPlan, string Schedule);

    public static class SheetConstants
    {
        public const string Url1 =
            "https://docs.google.com/spreadsheets/d/1Q9imoj8xLFgp887NsYeW8ngJ53E5GHvKblrnfatEBHk/edit#gid=";

        public const string Actual2022SpringUrl =
            "https://docs.google.com/spreadsheets/d/1wilGvPuDGr235DA8k3Wr0MHsUyC7rmVQz2iV1qABdJ4/edit#gid=";

        public const string SheetName = "Testing";
        public const string CredentialPath = "..\\..\\..\\..\\Credentials\\client_secrets.json";
        
        public const string InputRequirementsSheetName = "Входные требования";
        public const string InputRequirementsSheetName2 = "Входные требования осень";
        public const string InputRequirementsTestSheetName = "Входные требования Тест";
        public const string LearningPlanSheetName = "Учебный план";
        public const string LearningPlanSheetName2 = "Учебный план осень";
        public const string ScheduleSheetName = "Расписание";
        public const string ScheduleSheetName2 = "Расписание осень";
        public const string ClassroomsSheetName = "Аудитории";

        public static readonly SheetNamesConfig SpringConfig = new(Url1, InputRequirementsSheetName,
            LearningPlanSheetName,
            ScheduleSheetName);
        
        public static readonly SheetNamesConfig ActualSpring2022Config = new(Actual2022SpringUrl, InputRequirementsSheetName,
            LearningPlanSheetName,
            ScheduleSheetName);

        public static readonly SheetNamesConfig AutumnConfig = new(Url1, InputRequirementsSheetName2,
            LearningPlanSheetName2,
            ScheduleSheetName2);

        public static readonly GsRepository TestRepository = new("test", CredentialPath, Url1);

        public const string RowMeetingsUrl =
            "https://docs.google.com/spreadsheets/d/1HbiSlAkAI6yWxQpbeSsO6iBSrl45NIaw9sPCn4KXU1I/edit#gid=";

        public static readonly GsRepository RowMeetingsRepository = new("RowMeetings", CredentialPath, RowMeetingsUrl);
    }
}