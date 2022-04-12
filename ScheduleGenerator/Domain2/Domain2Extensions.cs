using CommonDomain.Enums;
using Google.Apis.Sheets.v4.Data;

namespace Domain2;

public static class Domain2Extensions
{
    public static Meeting2?[] GetDisciplineColumn(this IEnumerable<Meeting2> meetings, int dayDuration)
    {
        var column = new Meeting2?[dayDuration];
        foreach (var meeting in meetings)
        {
            for (var i = 0; i < meeting.Duration; i++)
            {
                var index = meeting.MeetingTime!.TimeSlot + i;
                if (column[index] == null)
                {
                    column[index] = meeting;
                }
                else
                {
                    Console.Error.WriteLine($"Discipline: {meeting.Discipline}");
                    Console.Error.WriteLine($"Time slot: {meeting.MeetingTime!.TimeSlot}");
                    Console.Error.WriteLine($"MeetingTime1: {column[index]!.MeetingTime}");
                    Console.Error.WriteLine($"MeetingTime2: {meeting.MeetingTime}");
                    throw new ArgumentException("Two meetings with same discipline in same time");
                }
            }
        }

        return column;
    }

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

    private static bool IsIntersectedWith(this Meeting2?[] column1, Meeting2?[] column2)
    {
        var length = column1.Length;
        for (var i = 0; i < length; i++)
        {
            if (column1[i] != null && column2[i] != null) return true;
        }

        return false;
    }

    private static void MergeColumnInto(this Meeting2?[] source, Meeting2?[] target)
    {
        for (var k = 0; k < source.Length; k++)
        {
            target[k] ??= source[k];
        }
    }

    public static string ToRuString(this WeekType weekType)
    {
        if (weekType == WeekType.Even) return "чётным";
        if (weekType == WeekType.Odd) return "нечётным";
        return weekType.ToString();
    }

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
}