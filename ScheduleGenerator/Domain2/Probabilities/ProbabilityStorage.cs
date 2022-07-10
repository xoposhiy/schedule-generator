using CommonDomain;
using CommonDomain.Enums;

namespace Domain2;

/// <summary>
/// Место, где зафиксированны пожелания студентов или их фактическое зачисления на курсы
/// </summary>
public class ProbabilityStorage
{
    private readonly Dictionary<Discipline, int> disciplineToMaxGroups = new();

    private readonly Dictionary<(string, Discipline), double[]> groupOfStudentOnDiscipline = new();

    private readonly bool isFinal;

    /// <summary>
    /// Соотношение приоритета студента к вероятности попасть на курс
    /// </summary>
    public readonly Dictionary<int, double> PriorityToProbability = new();

    /// <summary>
    /// Соотношение приоритета стедента к вероятности попасть на курс с тестовым
    /// </summary>
    public readonly Dictionary<int, double> PriorityWithEntranceToProbability = new();

    /// <summary>
    /// Кеш для матожидания студентов на предмете
    /// </summary>
    private readonly Dictionary<Discipline, double> studentsExpectation = new();

    /// <summary>
    /// Кеш для матожидания пересечения студентов, ходящих на оба предмета
    /// </summary>
    private readonly Dictionary<(Discipline, Discipline), double> studentsIntersectionExpectation = new();

    private readonly Dictionary<string, Dictionary<Discipline, StudentPriorities>> studentWithDisciplineToPriority =
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
    public void AddSubjectForStudent(string student, Discipline discipline, StudentPriorities priority)
    {
        if (!studentWithDisciplineToPriority.ContainsKey(student))
            studentWithDisciplineToPriority.Add(student, new());
        studentWithDisciplineToPriority[student].Add(discipline, priority);

        if (!studentsExpectation.ContainsKey(discipline))
            studentsExpectation[discipline] = 0;

        if (isFinal)
            studentsExpectation[discipline] += priority.Enlisted ? 1 : 0;
        else
            studentsExpectation[discipline] += GetPriorityDict(discipline)[priority.FormPriority];
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
            var groupsCount = GetDisciplineGroupCount(discipline);
            var groupsDistribution = Enumerable
                .Repeat(1d / groupsCount, groupsCount)
                .ToArray();
            var students = GetAllEnlistedStudents(discipline);
            foreach (var student in students) groupOfStudentOnDiscipline[(student, discipline)] = groupsDistribution;
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
                return studentsExpectation[meeting.Discipline];
            case DisciplineType.Obligatory:
                return StudentsCount;
            default:
                throw new ArgumentOutOfRangeException(nameof(meeting));
        }
    }

    /// <summary>
    /// Считает ожидаемое количество студентов, которые ходять на обе пары
    /// </summary>
    /// <param name="firstMeeting">Первая пара</param>
    /// <param name="secondMeeting">Вторая пара</param>
    /// <returns>Количество студентов, которые ходят на обе пары</returns>
    public double GetCommonStudents(Meeting2 firstMeeting, Meeting2 secondMeeting)
    {
        var isSameDiscipline = firstMeeting.Discipline == secondMeeting.Discipline;

        var expectation = 0d;

        foreach (var student in Students)
        {
            var firstProbability = GetProbabilityToBeOnMeeting(student, firstMeeting);
            var secondProbability = GetProbabilityToBeOnMeeting(student, secondMeeting, isSameDiscipline);

            expectation += firstProbability * secondProbability;
        }

        return expectation;
    }

    private double GetProbabilityToBeOnMeeting(string student, Meeting2 meeting, bool isSameDiscipline = false)
    {
        var discipline = meeting.Discipline;
        if (!groupOfStudentOnDiscipline.TryGetValue((student, discipline), out var cache)) return 0;
        var probabilityToBe = meeting.Groups.Sum(group => cache[group - 1]);
        if (isSameDiscipline) return probabilityToBe;
        return probabilityToBe * GetProbabilityToBeOnDiscipline(student, discipline);
    }

    private double GetProbabilityToBeOnDiscipline(string student, Discipline discipline)
    {
        var priority = GetCurrentPriority(studentWithDisciplineToPriority[student][discipline]);
        return GetPriorityDict(discipline)[priority];
    }

    private int GetCurrentPriority(StudentPriorities priorities)
    {
        return priorities.FormPriority;
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
            if (!isFinal || dictionary[discipline].Enlisted)
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

    public void RemoveStudentFromGroup(string student, Discipline discipline)
    {
        var groupsCount = GetDisciplineGroupCount(discipline);
        var groupsDistribution = Enumerable
            .Repeat(1d / groupsCount, groupsCount)
            .ToArray();
        groupOfStudentOnDiscipline[(student, discipline)] = groupsDistribution;
    }
}