using System.Text;
using CommonInfrastructure.GoogleSheetsRepository;
using Domain2;
using Domain2.Algorithms;
using Infrastructure;

namespace ScheduleCLI2;

public static class Program
{
    private static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        var meetingsSource = "Форматированные пары весна";
        var repo = new GsRepository("main",
            SheetConstants.CredentialPath,
            "https://docs.google.com/spreadsheets/d/1tPmGnwmLYCauCkbXSbLceb2_kf8N7xGO-OVKrk2hE8c/edit#gid=");
        var state = SheetToRequisitionConverter.ReadState(repo, meetingsSource);

        var rooms = SheetToRequisitionConverter.ReadRooms(repo, "Аудитории");
        SheetToProbabilityConverter.ReadPriorities(repo, state.NotPlacedMeetings.Values, "Приоритеты для шатания");
        SheetToProbabilityConverter.ReadProbabilities(repo, "Вероятности Весна");

        state = SolveGreedy(state);

        var sheetName = "Лист4";
        Visualizer.DrawSchedule(repo, state, sheetName);
        Visualizer.UpdateMeetingsData(repo, meetingsSource, state);
    }

    private static State SolveGreedy(State state)
    {
        var estimator = new MeanStudentIntersectionEstimator();
        var greedySolver = new GreedySolver(estimator);

        return greedySolver.GetSolutions(state).First();
    }
}