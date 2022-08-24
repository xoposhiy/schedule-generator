using CommonDomain;
using CommonDomain.Enums;
using Domain2.Algorithms.Estimators;
using Domain2.Probabilities;

namespace Domain2;

public class StudentsGroupsDistributor
{
    private readonly IStateEstimator estimator;
    private readonly StudentsDistribution? studentsDistribution;

    public StudentsGroupsDistributor(IStateEstimator estimator, StudentsDistribution? studentsDistribution)
    {
        this.estimator = estimator;
        this.studentsDistribution = studentsDistribution;
    }

    public Dictionary<Discipline, List<List<string>>> DistributeStudentsByGroups(State state)
    {
        var disciplines = state.ProbabilityStorage.Disciplines;
        var result = new Dictionary<Discipline, List<List<string>>>();
        foreach (var discipline in disciplines) result[discipline] = DistributeStudentsGreedy(state, discipline);

        return result;
    }

    public Dictionary<string, Dictionary<string, List<List<ulong>>>> Distribute(State state)
    {
        var groups = DistributeStudentsByGroups(state);
        
        var studentsToPersonalNumbers = studentsDistribution!.Students
            .ToDictionary(s => s.FullName, s => s.PersonalNumber);

        var result = new Dictionary<string, Dictionary<string, List<List<ulong>>>>();
        
        foreach (var (discipline, groupByDiscipline) in groups)
        {
            var groupPersonalNumbers = groupByDiscipline
                .Select(g => g.Select(n => studentsToPersonalNumbers[n]).ToList()).ToList();
            result[discipline.Name] = new()
            {
                ["*"] = groupPersonalNumbers
            };
        }

        return result;
    }

    public bool IsTypedGroupsSelfIntersected(ILookup<MeetingType, List<int>> groupsByType)
    {
        foreach (var typedGroups in groupsByType)
        {
            var uniqueGroups = new List<List<int>>();
            foreach (var typedGroup in typedGroups)
            {
                typedGroup.Sort();
                if (uniqueGroups.Any(g => g.SequenceEqual(typedGroup)))
                {
                    return false;
                }

                uniqueGroups.Add(typedGroup);
            }
        }

        return true;
    }

    private List<List<string>> DistributeStudentsGreedy(State state, Discipline discipline)
    {
        var groupsCount = state.ProbabilityStorage.GetDisciplineGroupCount(discipline);
        var group = 0;
        var students = state.ProbabilityStorage.GetAllEnlistedStudents(discipline).ToHashSet();

        var groups = new List<List<string>>();
        var scoredStudents = new List<PriorityQueue<string, double>>();
        for (; group < groupsCount; group++)
        {
            groups.Add(new());
            scoredStudents.Add(GetGroupScoredStudents(state, discipline, group, students));
        }

        var taken = new HashSet<string>();

        while (students.Count != 0)
        {
            group = (group + 1) % groupsCount;
            var bestStudentForGroup = TakeBestStudent(scoredStudents[group], taken);
            if (bestStudentForGroup == null) break;
            groups[group].Add(bestStudentForGroup);
            taken.Add(bestStudentForGroup);
            state.ProbabilityStorage.SetStudentToGroup(bestStudentForGroup, discipline, group);
        }

        return groups;
    }

    private PriorityQueue<string, double> GetGroupScoredStudents(
        State state,
        Discipline discipline,
        int group,
        IEnumerable<string> studentsPool)
    {
        var students = new PriorityQueue<string, double>();
        foreach (var student in studentsPool)
        {
            state.ProbabilityStorage.SetStudentToGroup(student, discipline, group);
            var score = estimator.EstimateSchedule(state);
            state.ProbabilityStorage.SplitStudentEvenlyBetweenAllGroups(student, discipline);
            students.Enqueue(student, score);
        }

        return students;
    }

    private static string? TakeBestStudent(PriorityQueue<string, double> students, ISet<string> takenStudents)
    {
        while (students.TryDequeue(out var student, out _))
            if (takenStudents.Add(student))
                return student;

        return null;
    }
}