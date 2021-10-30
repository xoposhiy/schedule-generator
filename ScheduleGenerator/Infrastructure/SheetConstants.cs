using Infrastructure.GoogleSheetsRepository;

namespace Infrastructure
{
    public record SheetNamesConfig(string Requirements, string LearningPlan, string Schedule);

    public static class SheetConstants
    {
        public const string Url =
            "https://docs.google.com/spreadsheets/d/1Q9imoj8xLFgp887NsYeW8ngJ53E5GHvKblrnfatEBHk/edit#gid=";

        public const string SheetName = "Testing";
        public const string CredentialPath = "..\\..\\..\\..\\Credentials\\client_secrets.json";

        public const string InputRequirementsSheetUrl = Url + "861045221";

        public const string InputRequirementsSheetName = "Входные требования";
        public const string InputRequirementsSheetName2 = "Входные требования осень";
        public const string InputRequirementsTestSheetName = "Входные требования Тест";
        public const string LearningPlanSheetName = "Учебный план";
        public const string LearningPlanSheetName2 = "Учебный план осень";
        public const string ScheduleSheetName = "Расписание";
        public const string ScheduleSheetName2 = "Расписание осень";
        public const string ClassroomsSheetName = "Аудитории";

        public static readonly SheetNamesConfig SpringConfig = new(InputRequirementsSheetName,
            LearningPlanSheetName,
            ScheduleSheetName);

        public static readonly SheetNamesConfig AutumnConfig = new(InputRequirementsSheetName2,
            LearningPlanSheetName2,
            ScheduleSheetName2);

        public static readonly GsRepository Repository = new("test", CredentialPath, Url);
    }
}