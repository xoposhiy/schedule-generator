using CommonDomain;

namespace Domain2;

public static class ProbabilityStorage
{
    public static readonly Dictionary<string, Dictionary<Discipline, int>> StudentWithDisciplineToPriority = new();
    public static readonly Dictionary<Discipline, List<HashSet<string>>> DisciplineWithPriorityToStudents = new();

    public static readonly Dictionary<int, double> PriorityWithEntranceToProbability = new();
    public static readonly Dictionary<int, double> PriorityCommonToProbability = new();

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
        var students1 = DisciplineWithPriorityToStudents[firstDiscipline];
        var students2 = DisciplineWithPriorityToStudents[secondDiscipline];
        var intersections = new List<HashSet<string>>();
        for (var prior = 0; prior < students1.Count; prior++)
        {
            var elem = students1[prior].ToHashSet();
            elem.IntersectWith(students2[prior]);
            intersections.Add(elem);
        }

        if (firstDiscipline.Type == secondDiscipline.Type)
        {
            if (firstDiscipline.Type == DisciplineType.WithEntranceTest)
                return intersections.Select((students, priority) =>
                    students.Count * PriorityWithEntranceToProbability[priority]).Sum();
            return intersections.Select((students, priority) =>
                students.Count * PriorityCommonToProbability[priority]).Sum();
        }

        return intersections.Select((students, priority) =>
            students.Count * (PriorityCommonToProbability[priority] + PriorityCommonToProbability[priority]) / 2).Sum();
    }
}