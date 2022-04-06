using CommonInfrastructure.GoogleSheetsRepository;
using Domain2;
using Infrastructure;
using Infrastructure.SheetPatterns;

namespace ScheduleCLI2;

public static class Program
{
    private static void Main()
    {
        var repo = new GsRepository("main",
            SheetConstants.CredentialPath,
            "https://docs.google.com/spreadsheets/d/1tPmGnwmLYCauCkbXSbLceb2_kf8N7xGO-OVKrk2hE8c/edit#gid=");
        var meetings = SheetToRequisitionConverter.ReadMeetings(repo, "Форматированные пары весна");
        var rooms = SheetToRequisitionConverter.ReadRooms(repo, "Аудитории");
    }
}