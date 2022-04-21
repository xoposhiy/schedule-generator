using CommonDomain;
using CommonDomain.Enums;

namespace Domain2;

public static class ProbabilityStorage
{
    public static readonly Dictionary<string, Dictionary<Discipline, int>> StudentWithDisciplineToPriority = new();
    public static readonly Dictionary<Discipline, List<HashSet<string>>> DisciplineWithPriorityToStudents = new();

    public static readonly Dictionary<int, double> PriorityWithEntranceToProbability = new();
    public static readonly Dictionary<int, double> PriorityCommonToProbability = new();

    public const double GroupCollisionsCoef = 1; //Скорее всего не единица :)

    public static void AddSubjectForStudent(string student, Discipline discipline, int priority)
    {
        if (!StudentWithDisciplineToPriority.ContainsKey(student))
            StudentWithDisciplineToPriority.Add(student, new Dictionary<Discipline, int>());
        StudentWithDisciplineToPriority[student].Add(discipline, priority);

        if (!DisciplineWithPriorityToStudents.ContainsKey(discipline))
            DisciplineWithPriorityToStudents.Add(discipline, Enumerable.Range(0, 5)
                .Select(_ => new HashSet<string>()).ToList());
        DisciplineWithPriorityToStudents[discipline][priority].Add(student);
    }

    public static double GetCommonStudents(this Meeting2 firstMeeting, Meeting2 secondMeeting)
    {
        var firstDiscipline = firstMeeting.Discipline;
        var secondDiscipline = secondMeeting.Discipline;
        var firstGroups = firstMeeting.Groups;
        var secondGroups = secondMeeting.Groups;
        var intersectionGroupsCount = 0;
        
        if (firstDiscipline == secondDiscipline && firstMeeting.MeetingType == secondMeeting.MeetingType)
        {
            intersectionGroupsCount = firstGroups.Intersect(secondGroups).Count();
        }
        
        var firstStudents = DisciplineWithPriorityToStudents[firstDiscipline];
        var secondStudents = DisciplineWithPriorityToStudents[secondDiscipline];
        var intersectionStudents = new List<HashSet<string>>();
        for (var prior = 0; prior < firstStudents.Count; prior++)
        {
            var students = firstStudents[prior].ToHashSet();
            students.IntersectWith(secondStudents[prior]);
            intersectionStudents.Add(students);
        }
        
        var softCoef = (firstGroups.Count - intersectionGroupsCount) * secondGroups.Count +
                       intersectionGroupsCount * (secondGroups.Count - 1);
        if (firstDiscipline.Type == secondDiscipline.Type)
        {
            return CalcStudents(intersectionStudents, softCoef, intersectionGroupsCount,
                firstDiscipline.Type == DisciplineType.WithEntranceTest 
                    ? PriorityWithEntranceToProbability 
                    : PriorityCommonToProbability);
        }

        return CalcStudents(intersectionStudents, softCoef, intersectionGroupsCount);
    }
    
    private static double CalcStudents(List<HashSet<string>> intersectionStudents, int softCoef,
        int intersectionGroupsCount)
    {
        return intersectionStudents.Select((students, priority) =>
            students.Count * PriorityCommonToProbability[priority] * PriorityWithEntranceToProbability[priority] * softCoef
            + students.Count * GroupCollisionsCoef * intersectionGroupsCount).Sum();
    }

    private static double CalcStudents(List<HashSet<string>> intersectionStudents, int softCoef,
        int intersectionGroupsCount, Dictionary<int, double> priorityToProbability)
    {
        return intersectionStudents.Select((students, priority) =>
            students.Count * priorityToProbability[priority] * priorityToProbability[priority] * softCoef
            + students.Count * GroupCollisionsCoef * intersectionGroupsCount).Sum();
    }
}