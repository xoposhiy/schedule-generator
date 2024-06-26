using System.Text;
using CommonDomain;
using CommonDomain.Enums;
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
        var termType = TermType.Spring;
        var termString = EnumHelper.GetTermString(termType);
        var sourceType = SourcePrioritiesType.JsonLk;
        
        
        var lkPriorities = JsonConvert.DeserializeObject<StudentsPriorities>(File.ReadAllText(Constants.PrioritiesJsonPath));
        foreach (var name in lkPriorities.MupIdToMupName.Values)
        {
            Console.WriteLine(name);
        }

        var repo = new GsRepository("main",
            SheetConstants.CredentialPath,
            "https://docs.google.com/spreadsheets/d/1yv7XiER-a20e3I7-3ILSRE4hPl-X8-168Tv6-M03HKk/edit#gid=");

        //var rooms = SheetToRequisitionConverter.ReadRooms(repo, "Аудитории");

        var parsingHelper = new ParsingHelper(termType, sourceType, repo);

        var meetingsSource = $"Форматированные пары ({termString})";
        var meetings = parsingHelper.ReadMeetings(meetingsSource);
        var disciplines = meetings.Select(m => m.Discipline).Distinct().ToDictionary(e => e.Name, e => e);
        
        var probabilityStorage = parsingHelper.ReadProbabilities();
        probabilityStorage.FillDisciplineToMaxGroups(meetings);
        
        SheetToProbabilityConverter.SetDisciplinesCount(disciplines.Count(e => e.Value.Type != DisciplineType.Obligatory));

        var (priorities, studentsDistribution) = parsingHelper.ReadPriorities(disciplines);
        foreach (var priority in priorities)
        {
            probabilityStorage.AddSubjectForStudent(priority);
        }

        probabilityStorage.InitStudentUniformDistribution();

        var state = new State(meetings, probabilityStorage);
        var solution = SolveByChokudai(state);
        //var solution = SolveRepeater(state);
        Console.Error.WriteLine($"Best score: {solution.Item2}");

        var sheetName = $"Расписание ({termString})";
        Visualizer.DrawSchedule(repo, solution.Item1, sheetName);
        Visualizer.UpdateMeetingsData(repo, meetingsSource, solution.Item1);
        LogEstimatorScores(state, solution.Item1, GetEstimator());

        Console.WriteLine();

        WriteStudentsGroupsDistributionToConsole(studentsDistribution, sourceType, solution.Item1);
    }


    private static void WriteStudentsGroupsDistributionToConsole(
        StudentsDistribution? studentsDistribution,
        SourcePrioritiesType sourceType,
        State state)
    {
        var stateEstimator = new StateEstimator(GetEstimator());
        var studentsDistributor = new StudentsGroupsDistributor(stateEstimator, studentsDistribution);
        switch (sourceType)
        {
            case SourcePrioritiesType.GoogleSheet:
            {
                var studentsByGroups = studentsDistributor.DistributeStudentsByGroups(state);
                LogGroups(studentsByGroups);
                break;
            }
            case SourcePrioritiesType.JsonFinal:
            {
                var groups = studentsDistributor.Distribute(state);
                var jsonGroups = JsonConvert.SerializeObject(groups);
                var path = "student-subgroups.json";
                File.WriteAllText(path, jsonGroups);
                Console.WriteLine(Path.GetFullPath(path));
                break;
            }
            case SourcePrioritiesType.JsonLk:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(sourceType), sourceType, null);
        }
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

        var solutions = repeater.GetSolutions(state, 60000).ToList();
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

    // Баг с конфликтами с Obligatory-парами
    private static (State, double) SolveByChokudai(State state)
    {
        var estimator = GetEstimator();

        var chokudai = new ChokudaiSearch(estimator);

        var solutions = chokudai.GetSolutions(state, 60_000).ToList();
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