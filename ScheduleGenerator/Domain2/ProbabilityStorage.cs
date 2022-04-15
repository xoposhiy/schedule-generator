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
            DisciplineWithPriorityToStudents.Add(discipline, Enumerable.Range(0,5)
                .Select(_ => new HashSet<string>()).ToList());
        DisciplineWithPriorityToStudents[discipline][priority].Add(student);
    }

    public static double GetCommonStudents(Meeting2 firstMeeting, Meeting2 secondMeeting)
    {
        return 0;
    }
    
}