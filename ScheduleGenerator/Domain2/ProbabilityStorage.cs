using CommonDomain;
using CommonDomain.Enums;

namespace Domain2;

public static class ProbabilityStorage
{
    private static readonly Dictionary<string, Dictionary<Discipline, int>> StudentWithDisciplineToPriority = new();
    private static readonly Dictionary<Discipline, List<HashSet<string>>> DisciplineWithPriorityToStudents = new();


    public static readonly Dictionary<int, double> PriorityWithEntranceToProbability = new();
    public static readonly Dictionary<int, double> PriorityCommonToProbability = new();

    private static readonly Dictionary<Discipline, int> DisciplineToMaxGroups = new();

    public static int StudentsCount => StudentWithDisciplineToPriority.Count;

    public static void AddSubjectForStudent(string student, Discipline discipline, int priority)
    {
        if (!StudentWithDisciplineToPriority.ContainsKey(student))
            StudentWithDisciplineToPriority.Add(student, new Dictionary<Discipline, int>());
        StudentWithDisciplineToPriority[student].Add(discipline, priority);

        if (!DisciplineWithPriorityToStudents.ContainsKey(discipline))
            DisciplineWithPriorityToStudents.Add(discipline, Enumerable.Range(0, 6)
                .Select(_ => new HashSet<string>()).ToList());
        DisciplineWithPriorityToStudents[discipline][priority].Add(student);
    }

    public static void FillDisciplineToMaxGroups(Dictionary<int, Meeting2>.ValueCollection meetings)
    {
        var dict = new Dictionary<Discipline, HashSet<int>>();
        foreach (var meeting in meetings)
        {
            if (!dict.ContainsKey(meeting.Discipline))
                dict.Add(meeting.Discipline, new HashSet<int>());
            dict[meeting.Discipline].UnionWith(meeting.Groups.ToHashSet());
        }

        foreach (var (discipline, groups) in dict)
            DisciplineToMaxGroups.Add(discipline, groups.Count);
    }

    public static double GetCommonStudents(this Meeting2 firstMeeting, Meeting2 secondMeeting)
    {
        var firstDiscipline = firstMeeting.Discipline;
        var secondDiscipline = secondMeeting.Discipline;
        var firstGroups = firstMeeting.Groups;
        var secondGroups = secondMeeting.Groups;
        int intersectionGroupsCount;
        var softCoef = 1d;
        var sameDiscipline = firstDiscipline == secondDiscipline;

        if (sameDiscipline)
        {
            intersectionGroupsCount = firstGroups.Intersect(secondGroups).Count();
            softCoef = (double) intersectionGroupsCount / DisciplineToMaxGroups[firstDiscipline];
        }

        var ans = 0d;
        foreach (var student in StudentWithDisciplineToPriority.Keys)
        {
            var firstPrior = StudentWithDisciplineToPriority[student][firstDiscipline];
            var secondPrior = StudentWithDisciplineToPriority[student][secondDiscipline];
            if (firstDiscipline.Type == secondDiscipline.Type)
            {
                ans += CalcStudents(firstPrior, secondPrior, softCoef,
                    GetPriorityDict(firstDiscipline), sameDiscipline);
            }
            else
            {
                ans += CalcStudents(firstPrior, secondPrior,
                    GetPriorityDict(firstDiscipline),
                    GetPriorityDict(secondDiscipline),
                    softCoef);
            }
        }

        return ans;
    }

    private static Dictionary<int, double> GetPriorityDict(Discipline discipline)
    {
        return discipline.Type == DisciplineType.WithEntranceTest
            ? PriorityWithEntranceToProbability
            : PriorityCommonToProbability;
    }

    private static double CalcStudents(int firstPrior, int secondPrior,
        double softCoef, Dictionary<int, double> priorityToProbability, bool sameDiscipline)
    {
        if (sameDiscipline) return priorityToProbability[firstPrior] * softCoef;
        return priorityToProbability[firstPrior] * priorityToProbability[secondPrior] * softCoef;
    }

    private static double CalcStudents(int firstPrior, int secondPrior,
        Dictionary<int, double> firstPriorityToProbability,
        Dictionary<int, double> secondPriorityToProbability, double softCoef)
    {
        return firstPriorityToProbability[firstPrior] * secondPriorityToProbability[secondPrior] * softCoef;
    }
}