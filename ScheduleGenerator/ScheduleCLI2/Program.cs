using CommonInfrastructure.GoogleSheetsRepository;
using Domain2;
using Infrastructure;

namespace ScheduleCLI2;

public static class Program
{
    private static void Main()
    {
        var meetingsSource = "Форматированные пары весна";
        var repo = new GsRepository("main",
            SheetConstants.CredentialPath,
            "https://docs.google.com/spreadsheets/d/1tPmGnwmLYCauCkbXSbLceb2_kf8N7xGO-OVKrk2hE8c/edit#gid=");
        var state = SheetToRequisitionConverter.ReadState(repo, meetingsSource);
        var disciplines = state.NotPlacedMeetings.Select(e => e.Discipline).ToHashSet();
        var rooms = SheetToRequisitionConverter.ReadRooms(repo, "Аудитории");
        SheetToProbabilityConverter.ReadPriorities(repo, disciplines, "Приоритеты для шатания");
        SheetToProbabilityConverter.ReadProbabilities(repo, "Вероятности Весна");
        
        while (state.NotPlacedMeetings.Count > 0)
        {
            var meeting = state.NotPlacedMeetings.First();
            state.PlaceMeeting(meeting, meeting.MeetingTimePriorities.First().First());
        }
        
        var sheetName = "Лист4";
        Visualizer.DrawSchedule(repo, state, sheetName);
        Visualizer.UpdateMeetingsData(repo, meetingsSource, state);
    }
}