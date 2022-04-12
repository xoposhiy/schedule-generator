using CommonInfrastructure.GoogleSheetsRepository;
using Google.Apis.Sheets.v4.Data;
using static CommonInfrastructure.Extensions;

namespace Domain2;

public static class Visualizer
{
    private const int RowOffset = 1;
    private const int ColumnOffset = 1;
    public static readonly SheetModifier.BordersWidths ThickBorders = new(0, 2, 2, 0);

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
            columnOffset += modifier.DrawMeetingsPerDay(meetingsSet, columnOffset, dayDuration);

        modifier.BuildTimeSlotsBar(0, RowOffset, 1, 1, dayDuration);
    }

    private static int DrawMeetingsPerDay(this SheetModifier modifier, IGrouping<DayOfWeek, Meeting2> meetings,
        int columnOffset, int dayDuration)
    {
        var day = meetings.Key.ToRuString();
        var columns = meetings
            .GroupBy(m => m.Discipline)
            .Select(g => g.GetDisciplineColumn(dayDuration))
            .ToList()
            .MergeColumns();
        var dayData = new List<List<CellData>> {new() {HeaderCellData(day)}};
        var width = columns.Count;

        modifier
            .ColorizeRange(RowOffset, columnOffset, dayDuration, width, BackgroundColor)
            .DrawColumns(columns, columnOffset, dayDuration)
            .WriteRange(0, columnOffset, dayData)
            .AddBorders(0, columnOffset)
            .MergeCell(0, columnOffset, 1, width)
            .AddBorders(RowOffset, columnOffset, dayDuration, width);

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

    private static CellData MeetingCellData(Meeting2 meeting)
    {
        var meetingString = meeting.ToString();
        var cellData = CommonCellData(meetingString);
        if (meeting.Place == "Онлайн")
            cellData.UserEnteredFormat.BackgroundColor = OnlineColor;

        var textFormatRun = meeting.GetMeetingTextFormatRun();
        cellData.TextFormatRuns = new List<TextFormatRun> {textFormatRun};

        return cellData;
    }
}