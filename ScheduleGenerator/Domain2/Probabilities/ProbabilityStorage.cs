using CommonDomain;
using CommonDomain.Enums;

namespace Domain2;

/// <summary>
/// Место, где в виде вероятностной модели зафиксированны пожелания студентов или их фактическое зачисления на курсы
/// </summary>
public class ProbabilityStorage
{
    private readonly Dictionary<Discipline, int> disciplineToMaxGroups = new();

    private readonly Dictionary<(string, Discipline), double[]> groupOfStudentOnDiscipline = new();

    private readonly bool isFinal;

    /// <summary>
    /// Выбранный студентом приоритет предмета (без тестового) -> статистическая вероятность того, что он на этот предмет попадет
    /// </summary>
    public readonly Dictionary<int, double> PriorityToProbability = new();

    /// <summary>
    /// Выбранный студентом приоритет предмета с тестовым -> статистическая вероятность того, что он на этот предмет попадет
    /// </summary>
    public readonly Dictionary<int, double> PriorityWithEntranceToProbability = new();

    /// <summary>
    /// Кеш для матожидания количества студентов на предмете
    /// </summary>
    private readonly Dictionary<Discipline, double> studentsExpectation = new();

    /// <summary>
    /// Кеш для матожидания количества студентов, ходящих на оба предмета
    /// </summary>
    private readonly Dictionary<(Discipline, Discipline), double> studentsIntersectionExpectation = new();

    private readonly Dictionary<string, Dictionary<Discipline, int>> studentWithDisciplineToPriority =
        new();

    public ProbabilityStorage(bool isFinal)
    {
        this.isFinal = isFinal;
    }

    public IEnumerable<string> Students => studentWithDisciplineToPriority.Keys;
    public int StudentsCount => studentWithDisciplineToPriority.Count;
    public IEnumerable<Discipline> Disciplines => disciplineToMaxGroups.Keys;

    /// <summary>
    /// Добовляет информацию о том, что студент записался на предмет discipline с приоритетом priority
    /// </summary>
    /// <param name="student">Имя студента</param>
    /// <param name="discipline">Дисциплина/предмет</param>
    /// <param name="priority">Приоритет студента</param>
    public void AddSubjectForStudent((string student, Discipline discipline, int priority) Tuple)
    {
        var (student, discipline, priority) = Tuple;
        if (!studentWithDisciplineToPriority.ContainsKey(student))
            studentWithDisciplineToPriority.Add(student, new());
        studentWithDisciplineToPriority[student].Add(discipline, priority);

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

    public void InitStudentUniformDistribution()
    {
        foreach (var discipline in Disciplines)
        {
            var students = GetAllEnlistedStudents(discipline);
            foreach (var student in students) SplitStudentEvenlyBetweenAllGroups(student, discipline);
        }
    }

    /// <summary>
    /// Считает ожидаемое количество студентов на паре
    /// </summary>
    /// <param name="meeting">Пара</param>
    /// <returns>Ожидаемое количество студентов</returns>
    /// <exception cref="ArgumentOutOfRangeException">Выбрасывется, когда вид пары не учтен</exception>
    public double GetStudentsExpectation(Meeting2 meeting)
    {
        switch (meeting.Discipline.Type)
        {
            case DisciplineType.Free:
            case DisciplineType.WithEntranceTest:
                return studentsExpectation.GetValueOrDefault(meeting.Discipline, 0);
            case DisciplineType.Obligatory:
                return StudentsCount;
            default:
                throw new ArgumentOutOfRangeException(nameof(meeting));
        }
    }

    /// <summary>
    /// Считает матожидание количества студентов, которые ходять на обе пары
    /// </summary>
    /// <param name="first">Первая пара</param>
    /// <param name="second">Вторая пара</param>
    /// <returns>Матожидание количества студентов, которые ходят на обе пары</returns>
    public double GetCommonStudents(Meeting2 first, Meeting2 second)
    {
        var expectation = 0d;

        foreach (var student in Students)
        {
            var bothProbability = GetProbabilityToBeOnBothDiscipline(student, first.Discipline, second.Discipline);
            var firstProbability = GetProbabilityToBeOnMeeting(student, first);
            var secondProbability = GetProbabilityToBeOnMeeting(student, second);

            expectation += firstProbability * secondProbability * bothProbability;
        }

        return expectation;
    }

    private double GetProbabilityToBeOnMeeting(string student, Meeting2 meeting)
    {
        var discipline = meeting.Discipline;
        if (!groupOfStudentOnDiscipline.TryGetValue((student, discipline), out var cache)) return 0;
        var probabilityToBe = meeting.Groups.Sum(group => cache[group - 1]);
        return probabilityToBe;
    }

    private double GetProbabilityToBeOnBothDiscipline(string student, Discipline first, Discipline second)
    {
        var firstProbability = GetProbabilityToBeOnDiscipline(student, first);
        if (first == second) return firstProbability;
        return firstProbability * GetProbabilityToBeOnDiscipline(student, second);
    }

    private double GetProbabilityToBeOnDiscipline(string student, Discipline discipline)
    {
        var priority = studentWithDisciplineToPriority[student].GetValueOrDefault(discipline, 5);
        return GetPriorityDict(discipline)[priority];
    }
    

    private Dictionary<int, double> GetPriorityDict(Discipline discipline)
    {
        return discipline.Type == DisciplineType.WithEntranceTest
            ? PriorityWithEntranceToProbability
            : PriorityToProbability;
    }

    public List<string> GetAllEnlistedStudents(Discipline discipline)
    {
        if (discipline.Type == DisciplineType.Obligatory)
            return Students.ToList();

        var students = new List<string>();
        foreach (var (student, dictionary) in studentWithDisciplineToPriority)
            if (dictionary.ContainsKey(discipline))
                students.Add(student);

        return students;
    }

    public int GetDisciplineGroupCount(Discipline discipline)
    {
        return disciplineToMaxGroups[discipline];
    }

    public void SetStudentToGroup(string student, Discipline discipline, int group)
    {
        var groupsCount = GetDisciplineGroupCount(discipline);
        var groupsDistribution = new double[groupsCount];
        groupsDistribution[group] = 1;
        groupOfStudentOnDiscipline[(student, discipline)] = groupsDistribution;
    }

    public void SplitStudentEvenlyBetweenAllGroups(string student, Discipline discipline)
    {
        var groupsCount = GetDisciplineGroupCount(discipline);
        var groupsDistribution = Enumerable
            .Repeat(1d / groupsCount, groupsCount)
            .ToArray();
        groupOfStudentOnDiscipline[(student, discipline)] = groupsDistribution;
    }
}