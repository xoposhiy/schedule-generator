using CommonDomain.Enums;
using Google.Apis.Sheets.v4.Data;

namespace Domain2;

public static class Domain2Extensions
{
    /// <summary>
    /// Создает виртуальные столбцы для предмета
    /// </summary>
    /// <param name="meetings">Пары одного предмета, идущие в один день</param>
    /// <param name="dayDuration">Продолжительность одного дня</param>
    /// <returns>Виртульные колонки в расписании</returns>
    public static IEnumerable<Meeting2?[]> GetDisciplineColumn(this IEnumerable<Meeting2> meetings, int dayDuration)
    {
        var columns = new List<Meeting2?[]>();
        foreach (var meeting in meetings.OrderBy(m => m.MeetingTime!.TimeSlot))
        {
            var column = new Meeting2?[dayDuration];
            var timeSlotIndex = meeting.MeetingTime!.TimeSlotIndex;
            for (var i = 0; i < meeting.Duration; i++)
            {
                column[timeSlotIndex + i] ??= meeting;
            }

            columns.Add(column);
        }

        return columns.MergeColumns();
    }

    /// <summary>
    /// По возможности слепляет колонки в меньшее количество
    /// </summary>
    /// <param name="columns">Заполненные колонки в расписании</param>
    /// <returns>Колонки в расписании</returns>
    public static List<Meeting2?[]> MergeColumns(this List<Meeting2?[]> columns)
    {
        var merged = new bool[columns.Count];
        for (var i = 0; i < columns.Count - 1; i++)
        {
            if (merged[i]) continue;
            for (var j = i + 1; j < columns.Count; j++)
            {
                if (merged[j] || columns[i].IsIntersectedWith(columns[j])) continue;
                merged[j] = true;
                columns[j].MergeColumnInto(columns[i]);
            }
        }

        return columns.Where((_, i) => !merged[i]).ToList();
    }

    /// <summary>
    /// Проеверяет, есть ли пересечение в колонках
    /// </summary>
    /// <param name="column1">Колонка1</param>
    /// <param name="column2">Колонка2</param>
    /// <returns>Есть ли пересечение</returns>
    private static bool IsIntersectedWith(this Meeting2?[] column1, Meeting2?[] column2)
    {
        var length = column1.Length;
        for (var i = 0; i < length; i++)
        {
            if (column1[i] != null && column2[i] != null) return true;
        }

        return false;
    }

    /// <summary>
    /// Вливает первую колонку во вторую
    /// </summary>
    /// <param name="source">Колонка, из которой надо взять пары</param>
    /// <param name="target">Колонка, в которую нужно добавить пары</param>
    private static void MergeColumnInto(this Meeting2?[] source, Meeting2?[] target)
    {
        for (var k = 0; k < source.Length; k++)
        {
            target[k] ??= source[k];
        }
    }

    /// <summary>
    /// Русская локализация
    /// </summary>
    /// <param name="weekType">Тип пары</param>
    /// <returns>Русская строка</returns>
    public static string ToRuString(this WeekType weekType)
    {
        if (weekType == WeekType.Even) return "четным";
        if (weekType == WeekType.Odd) return "нечетным";
        return weekType.ToString();
    }

    /// <summary>
    /// Отдает формат ячейки
    /// </summary>
    /// <param name="meeting2">Пара для ячейки которой будет применено форматирование</param>
    /// <returns>Формат ячейки</returns>
    public static TextFormatRun GetMeetingTextFormatRun(this Meeting2 meeting2)
    {
        var start = meeting2.ToString().IndexOf(meeting2.GetWeekTypeStringPart(), StringComparison.Ordinal);
        if (start < 0) return new();
        return new()
        {
            StartIndex = start,
            Format = new()
            {
                Bold = true
            }
        };
    }

    /// <summary>
    /// Для еще не составленного расписания отдает возможные пары для постановки их в рассписание
    /// </summary>
    /// <param name="state">Рассписание, в которое нужно добавить пару</param>
    /// <returns>Множество вариантов для постановки одной пары</returns>
    public static IEnumerable<Meeting2> GetPossibleVariants(this State state)
    {
        var meetingsToPlace = state.NotPlacedMeetings.Values.ToList();
        if (meetingsToPlace.Count == 0) yield break;

        var fixedMeeting = meetingsToPlace.FirstOrDefault(m => m.IsFixed);
        if (fixedMeeting != null)
        {
            yield return fixedMeeting;
            yield break;
        }

        var minPriority = meetingsToPlace.Min(m => m.Priority);
        foreach (var meeting in meetingsToPlace.Where(m => m.Priority == minPriority))
        foreach (var meetingMeetingTimePriority in meeting.MeetingTimePriorities)
        foreach (var meetingTime in meetingMeetingTimePriority)
            yield return meeting with {MeetingTime = meetingTime};
    }
}