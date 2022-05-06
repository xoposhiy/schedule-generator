using CommonDomain;
using CommonDomain.Enums;

namespace Domain2;

public class ProbabilityStorage
{
    private readonly Dictionary<Discipline, int> disciplineToMaxGroups = new();
    private readonly Dictionary<Discipline, List<HashSet<string>>> disciplineWithPriorityToStudents = new();
    public readonly Dictionary<int, double> PriorityToProbability = new();

    public readonly Dictionary<int, double> PriorityWithEntranceToProbability = new();

    private readonly Dictionary<Discipline, double> studentsExpectation = new();
    private readonly Dictionary<string, Dictionary<Discipline, int>> studentWithDisciplineToPriority = new();

    public int StudentsCount => studentWithDisciplineToPriority.Count;

    public void AddSubjectForStudent(string student, Discipline discipline, int priority)
    {
        if (!studentWithDisciplineToPriority.ContainsKey(student))
            studentWithDisciplineToPriority.Add(student, new Dictionary<Discipline, int>());
        studentWithDisciplineToPriority[student].Add(discipline, priority);

        if (!disciplineWithPriorityToStudents.ContainsKey(discipline))
            disciplineWithPriorityToStudents.Add(discipline, Enumerable.Range(0, 6)
                .Select(_ => new HashSet<string>()).ToList());
        disciplineWithPriorityToStudents[discipline][priority].Add(student);

        if (!studentsExpectation.ContainsKey(discipline))
            studentsExpectation[discipline] = 0;
        studentsExpectation[discipline] += GetPriorityDict(discipline)[priority];
    }

    public void FillDisciplineToMaxGroups(IEnumerable<Meeting2> meetings)
    {
        var dict = new Dictionary<Discipline, HashSet<int>>();
        foreach (var meeting in meetings)
        {
            if (!dict.ContainsKey(meeting.Discipline))
                dict.Add(meeting.Discipline, new());
            dict[meeting.Discipline].UnionWith(meeting.Groups.ToHashSet());
        }

        foreach (var (discipline, groups) in dict)
            disciplineToMaxGroups.Add(discipline, groups.Count);
    }

    public double GetStudentsExpectation(Meeting2 meeting)
    {
        switch (meeting.Discipline.Type)
        {
            case DisciplineType.Free:
            case DisciplineType.WithEntranceTest:
                return studentsExpectation[meeting.Discipline];
            case DisciplineType.Obligatory:
                return StudentsCount;
            default:
                throw new ArgumentOutOfRangeException(nameof(meeting));
        }
    }

    public double GetCommonStudents(Meeting2 firstMeeting, Meeting2 secondMeeting)
    {
        var firstDiscipline = firstMeeting.Discipline;
        var secondDiscipline = secondMeeting.Discipline;
        var firstGroups = firstMeeting.Groups;
        var secondGroups = secondMeeting.Groups;
        var groupsIntersectionCoef = 1d; //TODO WTF is this
        var sameDiscipline = firstDiscipline == secondDiscipline;

        if (sameDiscipline)
        {
            var intersectionGroupsCount = firstGroups.Intersect(secondGroups).Count();
            groupsIntersectionCoef = (double) intersectionGroupsCount / disciplineToMaxGroups[firstDiscipline];
        }

        var result = 0d;
        foreach (var student in studentWithDisciplineToPriority.Keys)
        {
            var firstPrior = studentWithDisciplineToPriority[student][firstDiscipline];
            var secondPrior = studentWithDisciplineToPriority[student][secondDiscipline];

            if (sameDiscipline)
                result += CalcStudents(firstPrior, groupsIntersectionCoef,
                    GetPriorityDict(firstDiscipline));
            else
                result += CalcStudents(firstPrior, secondPrior,
                    GetPriorityDict(firstDiscipline),
                    GetPriorityDict(secondDiscipline),
                    groupsIntersectionCoef);
        }

        return result;
    }

    private Dictionary<int, double> GetPriorityDict(Discipline discipline)
    {
        return discipline.Type == DisciplineType.WithEntranceTest
            ? PriorityWithEntranceToProbability
            : PriorityToProbability;
    }

    private static double CalcStudents(int firstPrior,
        double groupsIntersectionCoef, IReadOnlyDictionary<int, double> priorityToProbability)
    {
        return priorityToProbability[firstPrior] * groupsIntersectionCoef;
    }

    private static double CalcStudents(int firstPrior, int secondPrior,
        IReadOnlyDictionary<int, double> firstPriorityToProbability,
        IReadOnlyDictionary<int, double> secondPriorityToProbability, double groupsIntersectionCoef)
    {
        return firstPriorityToProbability[firstPrior] * secondPriorityToProbability[secondPrior] *
               groupsIntersectionCoef;
    }
}