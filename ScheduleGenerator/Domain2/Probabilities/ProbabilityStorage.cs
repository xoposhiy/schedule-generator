using CommonDomain;
using CommonDomain.Enums;

namespace Domain2;

/// <summary>
/// Место, где зафиксированны пожелания студентов или их фактическое зачисления на курсы
/// </summary>
public class ProbabilityStorage
{
    private readonly Dictionary<Discipline, int> disciplineToMaxGroups = new();

    private readonly bool IsFinal;

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
        IsFinal = isFinal;
    }

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

        if (IsFinal)
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
            var firstPrior = studentWithDisciplineToPriority[student][firstDiscipline].FormPriority;
            var secondPrior = studentWithDisciplineToPriority[student][secondDiscipline].FormPriority;

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

    public List<string> GetAllEnlistedStudents(Discipline discipline)
    {
        var students = new List<string>();
        foreach (var (student, dictionary) in studentWithDisciplineToPriority)
            if (dictionary[discipline].Enlisted)
                students.Add(student);

        return students;
    }

    public int GetDisciplineGroupCount(Discipline discipline)
    {
        return disciplineToMaxGroups[discipline];
    }

    public void SetStudentToGroup(string student, Discipline discipline, int group)
    {
        throw new NotImplementedException();
    }

    public void RemoveStudentFromGroup(string student, Discipline discipline, int group)
    {
        throw new NotImplementedException();
    }
}