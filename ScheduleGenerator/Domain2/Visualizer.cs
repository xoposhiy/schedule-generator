using CommonInfrastructure.GoogleSheetsRepository;
using Google.Apis.Sheets.v4.Data;
using static CommonInfrastructure.Extensions;

namespace Domain2;

public static class Visualizer
{
    private const int RowOffset = 1;
    private const int ColumnOffset = 1;

    public static void DrawMeetings(GsRepository repository, List<Meeting2> meetings, string sheetName)
    {
        repository.ClearSheet(sheetName);
        using var modifier = repository.ModifySpreadSheet(sheetName);
        var meetingsByDay = meetings
            .Where(m => !m.Ignore)
            .GroupBy(m => m.MeetingTime!.DayOfWeek)
            .OrderBy(g => g.Key);

        var columnOffset = ColumnOffset;
        foreach (var meetingsSet in meetingsByDay)
            columnOffset += modifier.DrawMeetingsPerDay(meetingsSet.ToList(), columnOffset);

        modifier.BuildTimeSlotsBar(0, RowOffset, 1, 1, Constants.TimeSlots);
    }

    private static int DrawMeetingsPerDay(this SheetModifier modifier, List<Meeting2> meetings, int columnOffset)
    {
        var dayDuration = meetings.Max(m => m.MeetingTime!.TimeSlot + m.Duration - 1) + 1;
        var meetingsByDiscipline = meetings.GroupBy(m => m.Discipline)
            .Select(g => GetDisciplineColumn(g.ToList(), dayDuration))
            .ToList();
        var columns = MergeColumns(meetingsByDiscipline);
        for (int i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            for (int y = 0; y < dayDuration; y++)
            {
                var meeting = column[y];
                if (meeting == null) continue;
                var meetingData = new List<List<CellData>> {new() {MeetingCellData(meeting)}};
                var height = meeting.Duration;

                var startRow = RowOffset + y;
                var startColumn = columnOffset + i;
                modifier
                    .WriteRange(startRow, startColumn, meetingData)
                    .MergeCell(startRow, startColumn, height, 1);
                y += height - 1;
            }
        }

        var day = meetings.First().MeetingTime!.DayOfWeek.ToString();
        var dayData = new List<List<CellData>> {new() {HeaderCellData(day)}};
        modifier
            .WriteRange(0, columnOffset, dayData)
            .AddBorders(0, columnOffset)
            .MergeCell(0, columnOffset, 1, columns.Count);

        modifier.Execute();
        Thread.Sleep(1);
        return columns.Count;
    }

    private static Meeting2?[] GetDisciplineColumn(List<Meeting2> meetings, int dayDuration)
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
                    throw new ArgumentException("Same discipline in same time");
                }
            }
        }

        return column;
    }

    private static List<Meeting2?[]> MergeColumns(List<Meeting2?[]> columns)
    {
        var merged = new bool[columns.Count];
        for (int i = 0; i < columns.Count - 1; i++)
        {
            if (merged[i]) continue;
            for (int j = i + 1; j < columns.Count; j++)
            {
                if (merged[j]) continue;
                if (AreIntersect(columns[i], columns[j])) continue;
                merged[j] = true;
                for (int k = 0; k < columns[i].Length; k++)
                {
                    columns[i][k] ??= columns[j][k];
                }
            }
        }

        return columns.Where((_, i) => !merged[i]).ToList();
    }

    private static bool AreIntersect(Meeting2?[] column1, Meeting2?[] column2)
    {
        var length = column1.Length;
        var areIntersect = false;
        for (int i = 0; i < length; i++)
        {
            areIntersect |= column1[i] != null && column2[i] != null;
        }

        return areIntersect;
    }

    private static CellData MeetingCellData(Meeting2 meeting)
    {
        var value = MeetingToString(meeting);
        var cellData = CommonCellData(value);
        if (meeting.Place == "Онлайн")
            cellData.UserEnteredFormat.BackgroundColor = OnlineColor;

        return cellData;
    }

    private static string MeetingToString(Meeting2 meeting)
    {
        return meeting.Discipline.Name;
    }
}