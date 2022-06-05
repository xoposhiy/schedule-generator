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
    private readonly Dictionary<(Discipline, Discipline), double> studentsIntersectionExpectation = new();
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
            dict[meeting.Discipline].UnionWith(meeting.Groups);
        }

        foreach (var (discipline, groups) in dict)
            disciplineToMaxGroups.Add(discipline, groups.Max());
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

        if (firstDiscipline == secondDiscipline)
        {
            var intersectionGroupsCount = (double) firstGroups.Intersect(secondGroups).Count();
            var groupsIntersection = intersectionGroupsCount / disciplineToMaxGroups[firstDiscipline];
            return GetStudentsExpectation(firstMeeting) * groupsIntersection;
        }

        if (studentsIntersectionExpectation.TryGetValue((firstDiscipline, secondDiscipline), out var expectation))
            return expectation;

        var firstPriorityToProbability = GetPriorityDict(firstDiscipline);
        var secondPriorityToProbability = GetPriorityDict(secondDiscipline);
        foreach (var student in studentWithDisciplineToPriority.Keys)
        {
            var firstPrior = studentWithDisciplineToPriority[student][firstDiscipline];
            var secondPrior = studentWithDisciplineToPriority[student][secondDiscipline];

            expectation += firstPriorityToProbability[firstPrior] * secondPriorityToProbability[secondPrior];
        }

        studentsIntersectionExpectation[(firstDiscipline, secondDiscipline)] = expectation;
        studentsIntersectionExpectation[(secondDiscipline, firstDiscipline)] = expectation;

        return expectation;
    }

    private Dictionary<int, double> GetPriorityDict(Discipline discipline)
    {
        return discipline.Type == DisciplineType.WithEntranceTest
            ? PriorityWithEntranceToProbability
            : PriorityToProbability;
    }
}