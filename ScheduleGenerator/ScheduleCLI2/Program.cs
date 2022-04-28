using System.Text;
using CommonInfrastructure.GoogleSheetsRepository;
using Domain2;
using Domain2.Algorithms;
using Domain2.Algorithms.Estimators;
using Domain2.Algorithms.Solvers;
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
        SheetToProbabilityConverter.ReadPriorities(repo, state.ProbabilityStorage, state.NotPlacedMeetings.Values,
            "Приоритеты для шатания");
        SheetToProbabilityConverter.ReadProbabilities(repo, state.ProbabilityStorage, "Вероятности Весна");

        var solution = SolveGreedy(state);

        var sheetName = "Лист4";
        Visualizer.DrawSchedule(repo, solution.Item1, sheetName);
        Visualizer.UpdateMeetingsData(repo, meetingsSource, solution.Item1);
    }

    private static (State, double) SolveGreedy(State state)
    {
        var estimator = new CombinedMeetingEstimator(new IMeetingEstimator[]{new MeanStudentIntersectionEstimator(), new TimePriorityEstimator()});
        var greedySolver = new GreedySolver(estimator);

        return greedySolver.GetSolutions(state).First();
    }
}