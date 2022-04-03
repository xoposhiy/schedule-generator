using Domain2;
using Infrastructure.GoogleSheetsRepository;
using Infrastructure;
using Infrastructure.SheetPatterns;

namespace ScheduleCLI2;

public static class Program
{
    private static void Main()
    {
        var repository = new GsRepository("main",
            SheetConstants.CredentialPath,
            "https://docs.google.com/spreadsheets/d/1tPmGnwmLYCauCkbXSbLceb2_kf8N7xGO-OVKrk2hE8c/edit#gid=");
        SheetToRequisitionConverter.ConvertToRequisitions(repository, "Форматированные пары весна", "");
    }
}