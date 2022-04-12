using CommonInfrastructure.GoogleSheetsRepository;
using Google.Apis.Sheets.v4.Data;
using static CommonInfrastructure.Extensions;

namespace Domain2;

public static class Visualizer
{
    private const int RowOffset = 1;
    private const int ColumnOffset = 1;

    public static void DrawSchedule(GsRepository repository, List<Meeting2> meetings, string sheetName)
    {
        var dayDuration = meetings.Max(m => m.MeetingTime!.TimeSlot + m.Duration - 1) + 1;
        var meetingsByDay = meetings
            .Where(m => !m.Ignore)
            .GroupBy(m => m.MeetingTime!.DayOfWeek)
            .OrderBy(g => g.Key);

        repository.ClearSheet(sheetName);
        using var modifier = repository.ModifySpreadSheet(sheetName);
        var columnOffset = ColumnOffset;
        foreach (var meetingsSet in meetingsByDay)
            columnOffset += modifier.DrawMeetingsPerDay(meetingsSet.ToList(), columnOffset, dayDuration);

        modifier.BuildTimeSlotsBar(0, RowOffset, 1, 1, dayDuration);
    }

    private static int DrawMeetingsPerDay(this SheetModifier modifier, List<Meeting2> meetings, int columnOffset,
        int dayDuration)
    {
        var day = meetings.First().MeetingTime!.DayOfWeek.ToString();
        var meetingsByDiscipline = meetings
            .GroupBy(m => m.Discipline)
            .Select(g => GetDisciplineColumn(g.ToList(), dayDuration))
            .ToList();
        var columns = MergeColumns(meetingsByDiscipline);
        var dayData = new List<List<CellData>> {new() {HeaderCellData(day)}};
        var width = columns.Count;

        modifier
            .ColorizeRange(RowOffset, columnOffset, dayDuration, width, BackgroundColor)
            .DrawColumns(columns, columnOffset, dayDuration)
            .WriteRange(0, columnOffset, dayData)
            .AddBorders(0, columnOffset)
            .MergeCell(0, columnOffset, 1, width);

        return width;
    }

    private static SheetModifier DrawColumns(this SheetModifier modifier, List<Meeting2?[]> columns, int columnOffset,
        int dayDuration)
    {
        for (var i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            for (var y = 0; y < dayDuration; y++)
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

        modifier.Execute();
        return modifier;
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
                    throw new ArgumentException("Two meetings with same discipline in same time");
                }
            }
        }

        return column;
    }

    private static List<Meeting2?[]> MergeColumns(List<Meeting2?[]> columns)
    {
        var merged = new bool[columns.Count];
        for (var i = 0; i < columns.Count - 1; i++)
        {
            if (merged[i]) continue;
            for (var j = i + 1; j < columns.Count; j++)
            {
                if (merged[j] || AreIntersect(columns[i], columns[j])) continue;
                merged[j] = true;
                columns[j].MergeColumnInto(columns[i]);
            }
        }

        return columns.Where((_, i) => !merged[i]).ToList();
    }

    private static bool AreIntersect(Meeting2?[] column1, Meeting2?[] column2)
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

    private static CellData MeetingCellData(Meeting2 meeting)
    {
        var cellData = CommonCellData(meeting.ToString());
        if (meeting.Place == "Онлайн")
            cellData.UserEnteredFormat.BackgroundColor = OnlineColor;

        return cellData;
    }
}