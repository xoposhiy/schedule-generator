using System.Text;
using CommonInfrastructure.GoogleSheetsRepository;
using Domain2;
using Domain2.Algorithms.Estimators;
using Domain2.Algorithms.Solvers;
using Infrastructure;

namespace ScheduleCLI2;

public static class Program
{
    private static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        // var regime = "autumn";
        var regime = "spring";


        var repo = new GsRepository("main",
            SheetConstants.CredentialPath,
            "https://docs.google.com/spreadsheets/d/1tPmGnwmLYCauCkbXSbLceb2_kf8N7xGO-OVKrk2hE8c/edit#gid=");

        var rooms = SheetToRequisitionConverter.ReadRooms(repo, "Аудитории");

        var meetingsSource = regime == "autumn" ? "Форматированные пары осень" : "Форматированные пары весна";
        var probabilitiesSource = regime == "autumn" ? "Вероятности Осень" : "Вероятности Весна";
        var meetings = SheetToRequisitionConverter.ReadMeetings(repo, meetingsSource);
        var probabilityStorage = SheetToProbabilityConverter.ReadProbabilities(repo, probabilitiesSource);
        var state = new State(meetings, probabilityStorage);

        if (regime == "autumn")
        {
            SheetToProbabilityConverter.SetDisciplinesCount(18);
            SheetToProbabilityConverter.ReadPriorities(repo, state.ProbabilityStorage, state.NotPlacedMeetings.Values,
                "Приоритеты (Осень)");
        }
        else
        {
            SheetToProbabilityConverter.SetDisciplinesCount(23);
            SheetToProbabilityConverter.ReadPriorities(repo, state.ProbabilityStorage, state.NotPlacedMeetings.Values,
                "Приоритеты для шатания");
        }


        var solution = SolveByChokudai(state);
        Console.Error.WriteLine($"Best score: {solution.Item2}");

        var sheetName = "Лист4";
        Visualizer.DrawSchedule(repo, solution.Item1, sheetName);
        Visualizer.UpdateMeetingsData(repo, meetingsSource, solution.Item1);
    }

    private static (State, double) SolveGreedy(State state)
    {
        var estimator = new CombinedMeetingEstimator(new IMeetingEstimator[]
            {new MeanStudentIntersectionEstimator(), new TimePriorityEstimator()});
        var greedySolver = new GreedySolver(estimator);

        return greedySolver.GetSolutions(state, 0).First();
    }

    private static (State, double) SolveByChokudai(State state)
    {
        var estimator = new CombinedMeetingEstimator(new IMeetingEstimator[]
            {new MeanStudentIntersectionEstimator(), new TimePriorityEstimator()});

        var chokudai = new ChokudaiSearch(estimator);

        var solutions = chokudai.GetSolutions(state, 10000).ToList();
        foreach (var grouping in solutions.ToLookup(t => t.score))
        {
            Console.WriteLine(grouping.Key + " " + grouping.Count());
        }

        Console.Error.WriteLine($"Solutions count: {solutions.Count}");
        var best = solutions.MaxBy(s => s.score);
        Console.Error.WriteLine($"Best Generation: {solutions.FindIndex(s => s == best)}");
        return best;
    }
}