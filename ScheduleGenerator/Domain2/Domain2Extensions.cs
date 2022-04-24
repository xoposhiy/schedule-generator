using CommonDomain.Enums;
using Google.Apis.Sheets.v4.Data;

namespace Domain2;

public static class Domain2Extensions
{
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

    public static IEnumerable<Meeting2> GetAllPossibleVariants(this State state)
    {
        var fixedMeeting = state.NotPlacedMeetings.Values.FirstOrDefault(m => m.IsFixed);
        if (fixedMeeting != null)
        {
            yield return fixedMeeting;
            yield break;
        }

        var minPriority = state.NotPlacedMeetings.Values.Min(m => m.Priority);
        foreach (var meeting in state.NotPlacedMeetings.Values.Where(m => m.Priority == minPriority))
        foreach (var meetingMeetingTimePriority in meeting.MeetingTimePriorities)
        foreach (var meetingTime in meetingMeetingTimePriority)
            yield return meeting with {MeetingTime = meetingTime};
    }
}