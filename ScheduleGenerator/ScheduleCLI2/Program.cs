using CommonInfrastructure.GoogleSheetsRepository;
using Domain2;
using Domain2.Algorithms;
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
        var disciplines = state.NotPlacedMeetings.Values.Select(e => e.Discipline).ToHashSet();
        var rooms = SheetToRequisitionConverter.ReadRooms(repo, "Аудитории");
        SheetToProbabilityConverter.ReadPriorities(repo, disciplines, "Приоритеты для шатания");
        SheetToProbabilityConverter.ReadProbabilities(repo, "Вероятности Весна");

        SolveGreedy(state);

        var sheetName = "Лист4";
        Visualizer.DrawSchedule(repo, state, sheetName);
        Visualizer.UpdateMeetingsData(repo, meetingsSource, state);
    }

    private static State SolveGreedy(State state)
    {
        var randomEstimator = new MeanStudentIntersectionEstimator();
        var greedySolver = new GreedySolver(randomEstimator);

        return greedySolver.GetSolutions(state).First();
    }
}