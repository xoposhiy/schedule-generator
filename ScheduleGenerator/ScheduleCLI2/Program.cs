using System.Text;
using CommonDomain;
using CommonInfrastructure.GoogleSheetsRepository;
using Domain2;
using Domain2.Algorithms.Estimators;
using Domain2.Algorithms.Solvers;
using Domain2.Converters;
using Domain2.Probabilities;
using Infrastructure;
using Newtonsoft.Json;

namespace ScheduleCLI2;

public static class Program
{
    private static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        // var regime = "Осень";
        var regime = "Весна";
        var isFinal = false;

        var sourceType = SourcePrioritiesType.GoogleSheet;

        var repo = new GsRepository("main",
            SheetConstants.CredentialPath,
            "https://docs.google.com/spreadsheets/d/1tPmGnwmLYCauCkbXSbLceb2_kf8N7xGO-OVKrk2hE8c/edit#gid=");

        var rooms = SheetToRequisitionConverter.ReadRooms(repo, "Аудитории");

        var meetingsSource = $"Форматированные пары ({regime})";
        var probabilitiesSource = $"Вероятности ({regime})";
        var disciplineCount = regime == "Осень" ? 18 : 23;
        var prioritiesSource = $"Приоритеты ({regime})";
        var meetings = SheetToRequisitionConverter.ReadMeetings(repo, meetingsSource);
        var disciplines = meetings.Select(m => m.Discipline).Distinct().ToDictionary(e => e.Name, e => e);
        var probabilityStorage = SheetToProbabilityConverter.ReadProbabilities(repo, probabilitiesSource, isFinal);
        var state = new State(meetings, probabilityStorage);
        probabilityStorage.FillDisciplineToMaxGroups(meetings);
        SheetToProbabilityConverter.SetDisciplinesCount(disciplineCount);
        List<(string Student, Discipline Discipline, int priority)> priorities;
        switch (sourceType)
        {
            case SourcePrioritiesType.GoogleSheet:
                priorities = SheetToProbabilityConverter.ReadPriorities(repo, disciplines.Values.ToHashSet(), prioritiesSource).ToList();
                break;
            case SourcePrioritiesType.JsonLk:
                throw new NotImplementedException();
            // ReSharper disable once UnreachableSwitchCaseDueToIntegerAnalysis
            case SourcePrioritiesType.JsonFinal:
                var content = File.ReadAllText("Probabilities/students_distribution.json");
                var f = JsonConvert.DeserializeObject<StudentsDistribution>(content);
                priorities = new List<(string Student, Discipline Discipline, int priority)>();
                foreach (var studentChoices in f.Students)
                {
                    foreach (var mupId in studentChoices.MupIds)
                    {
                        var disciplineName = f.MupIdToMupName[mupId];
                        if (disciplines.TryGetValue(disciplineName, out var dics))
                        {
                            priorities.Add((studentChoices.FullName, dics, 1));
                        }
                        else
                        {
                            throw new ArgumentException($"Unknown discipline name: {disciplineName}");
                        }
                    }
                }
                
                break;
            default:
                throw new ArgumentException("Not supported source type");
        }
        foreach (var priority in priorities)
        {
            probabilityStorage.AddSubjectForStudent(priority);
        }

        probabilityStorage.InitStudentUniformDistribution();

        var solution = SolveByChokudai(state);
        //var solution = SolveRepeater(state);
        Console.Error.WriteLine($"Best score: {solution.Item2}");

        var sheetName = $"Расписание ({regime})";
        Visualizer.DrawSchedule(repo, solution.Item1, sheetName);
        Visualizer.UpdateMeetingsData(repo, meetingsSource, solution.Item1);
        LogEstimatorScores(state, solution.Item1, GetEstimator());

        var stateEstimator = new StateEstimator(GetEstimator());
        var studentsDistributor = new StudentsDistributor(stateEstimator);
        var studentsByGroups = studentsDistributor.DistributeStudentsByGroups(solution.Item1);
        LogGroups(studentsByGroups);
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

    private static void LogEstimatorScores(State problem, State solution, IMeetingEstimator estimator)
    {
        var scoredMeetings = new PriorityQueue<Meeting2, double>();
        foreach (var meeting in solution.PlacedMeetings)
        {
            var score = estimator.EstimateMeeting(problem, meeting);
            scoredMeetings.Enqueue(meeting, score);
            problem = problem.AddMeeting(meeting);
        }

        while (scoredMeetings.TryDequeue(out var meeting, out var score))
            Console.WriteLine($"Meeting {meeting} at {meeting.MeetingTime} was placed with {score} score");
    }

    private static void LogGroups(Dictionary<Discipline, List<List<string>>> studentsByGroups)
    {
        foreach (var (discipline, groups) in studentsByGroups)
        {
            if (groups.Count == 1) continue;
            Console.WriteLine(discipline);
            foreach (var group in groups)
            {
                Console.WriteLine($"\t{groups.IndexOf(group)}:");
                foreach (var student in group) Console.WriteLine($"\t\t{student}");
            }
        }
    }
}