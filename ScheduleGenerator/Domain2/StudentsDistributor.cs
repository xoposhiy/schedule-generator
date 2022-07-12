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
        var groups = Enumerable.Repeat(0, groupsCount)
            .Select(_ => new List<string>())
            .ToList();
        var group = 0;
        var students = state.ProbabilityStorage.GetAllEnlistedStudents(discipline).ToHashSet();
        while (students.Count != 0)
        {
            var bestStudentForGroup = GetBestStudentForGroup(state, discipline, group, students);
            groups[group].Add(bestStudentForGroup);
            students.Remove(bestStudentForGroup);
            state.ProbabilityStorage.SetStudentToGroup(bestStudentForGroup, discipline, group);
            group = (group + 1) % groupsCount;
        }

        return groups;
    }

    private string GetBestStudentForGroup(State state, Discipline discipline, int group,
        IEnumerable<string> studentsPool)
    {
        var bestStudent = string.Empty;
        var bestScore = double.MinValue;
        foreach (var student in studentsPool)
        {
            state.ProbabilityStorage.SetStudentToGroup(student, discipline, group);
            var score = estimator.EstimateSchedule(state);
            state.ProbabilityStorage.SplitStudentEvenlyBetweenAllGroups(student, discipline);
            if (score > bestScore)
            {
                bestScore = score;
                bestStudent = student;
            }
        }

        return bestStudent;
    }
}