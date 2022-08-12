using CommonDomain;
using Domain2.Algorithms.Estimators;

namespace Domain2;

public class StudentsDistributor
{
    private readonly IStateEstimator estimator;

    public StudentsDistributor(IStateEstimator estimator)
    {
        this.estimator = estimator;
    }

    public Dictionary<Discipline, List<List<string>>> DistributeStudentsByGroups(State state)
    {
        var disciplines = state.ProbabilityStorage.Disciplines;
        var result = new Dictionary<Discipline, List<List<string>>>();
        foreach (var discipline in disciplines) result[discipline] = DistributeStudentsGreedy(state, discipline);

        return result;
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