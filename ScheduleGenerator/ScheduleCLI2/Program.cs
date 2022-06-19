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
        // var regime = "Осень";
        var regime = "Весна";


        var repo = new GsRepository("main",
            SheetConstants.CredentialPath,
            "https://docs.google.com/spreadsheets/d/1tPmGnwmLYCauCkbXSbLceb2_kf8N7xGO-OVKrk2hE8c/edit#gid=");

        var rooms = SheetToRequisitionConverter.ReadRooms(repo, "Аудитории");

        var meetingsSource = $"Форматированные пары ({regime})";
        var probabilitiesSource = $"Вероятности ({regime})";
        var disciplineCount = regime == "Осень" ? 18 : 23;
        var prioritiesSource = $"Приоритеты ({regime})";
        var meetings = SheetToRequisitionConverter.ReadMeetings(repo, meetingsSource);
        var probabilityStorage = SheetToProbabilityConverter.ReadProbabilities(repo, probabilitiesSource);
        var state = new State(meetings, probabilityStorage);

        SheetToProbabilityConverter.SetDisciplinesCount(disciplineCount);
        SheetToProbabilityConverter.ReadPriorities(repo, probabilityStorage, meetings, prioritiesSource);

        var solution = SolveByChokudai(state);
        //var solution = SolveRepeater(state);
        Console.Error.WriteLine($"Best score: {solution.Item2}");

        var sheetName = $"Расписание ({regime})";
        Visualizer.DrawSchedule(repo, solution.Item1, sheetName);
        Visualizer.UpdateMeetingsData(repo, meetingsSource, solution.Item1);
    }

    private static IMeetingEstimator GetEstimator()
    {
        return new CombinedMeetingEstimator(new IMeetingEstimator[]
        {
            new MeanStudentIntersectionEstimator(),
            new TimePriorityEstimator(),
            new LateMeetingsEstimator()
        });
    }

    public static (State, double) SolveRepeater(State state)
    {
        var estimator = GetEstimator();

        var repeater = new RepeaterSolver(new GreedySolver(estimator, 2));

        var solutions = repeater.GetSolutions(state, 10000).ToList();
        foreach (var grouping in solutions.ToLookup(t => t.score).OrderBy(g => g.Key))
            Console.WriteLine(grouping.Key + " " + grouping.Count());

        // foreach (var grouping in solutions.ToLookup(t => t.schedule.HashCode))
        //     Console.WriteLine(grouping.Key + " " + grouping.Count());
        Console.Error.WriteLine($"Solutions count: {solutions.Count}");
        var best = solutions.MaxBy(s => s.score);
        Console.Error.WriteLine($"Best Generation: {solutions.FindIndex(s => s == best)}");
        return best;
    }

    private static (State, double) SolveGreedy(State state, int randomTopN = 1)
    {
        var estimator = GetEstimator();
        var greedySolver = new GreedySolver(estimator, randomTopN);

        return greedySolver.GetSolutions(state, 0).First();
    }

    private static (State, double) SolveByChokudai(State state)
    {
        var estimator = GetEstimator();

        var chokudai = new ChokudaiSearch(estimator);

        var solutions = chokudai.GetSolutions(state, 10000).ToList();
        foreach (var grouping in solutions.ToLookup(t => t.score).OrderBy(g => g.Key))
            Console.WriteLine(grouping.Key + " " + grouping.Count());

        // foreach (var grouping in solutions.ToLookup(t => t.schedule.HashCode))
        //     Console.WriteLine(grouping.Key + " " + grouping.Count());
        Console.Error.WriteLine($"Solutions count: {solutions.Count}");
        var best = solutions.MaxBy(s => s.score);
        Console.Error.WriteLine($"Best Generation: {solutions.FindIndex(s => s == best)}");
        return best;
    }
}