using CommonInfrastructure.GoogleSheetsRepository;
using Google.Apis.Sheets.v4.Data;
using static CommonInfrastructure.Extensions;

namespace Domain2;

public static class Visualizer
{
    private const int RowOffset = 1;

    public static void DrawMeetings(GsRepository repository, List<Meeting2> meetings, string sheetName)
    {
        repository.ClearSheet(sheetName);
        using var modifier = repository.ModifySpreadSheet(sheetName);
        var meetingsByDay = meetings
            .Where(m => !m.Ignore)
            .GroupBy(m => m.MeetingTime!.DayOfWeek)
            .OrderBy(g => g.Key);

        var columnOffset = 0;
        foreach (var meetingsSet in meetingsByDay)
            columnOffset += modifier.DrawMeetingsPerDay(meetingsSet.ToList(), columnOffset);
    }

    public static int DrawMeetingsPerDay(this SheetModifier modifier, List<Meeting2> meetings, int columnOffset)
    {
        var meetingsPerTimeSlot = meetings
            .GroupBy(m => m.MeetingTime!.TimeSlot)
            .OrderBy(g => g.Key);

        var maximum = 0;
        foreach (var meetingsSet in meetingsPerTimeSlot)
        {
            var meetingsCount = modifier.DrawMeetingsPerTimeSlot(meetingsSet.ToList(), columnOffset);
            maximum = Math.Max(maximum, meetingsCount);
        }

        var day = meetings.First().MeetingTime!.DayOfWeek.ToString();
        var data = new List<List<CellData>> {new() {HeaderCellData(day)}};
        modifier
            .WriteRange(0, columnOffset, data)
            .MergeCell(0, columnOffset, 1, maximum);

        modifier.Execute();
        Thread.Sleep(1);
        return maximum;
    }

    public static int DrawMeetingsPerTimeSlot(this SheetModifier modifier, List<Meeting2> meetings, int columnOffset)
    {
        foreach (var meeting in meetings)
        {
            var data = new List<List<CellData>> {new() {MeetingCellData(meeting)}};
            var row = RowOffset + Math.Max(1, meeting.MeetingTime!.TimeSlot);
            modifier.WriteRange(row, columnOffset++, data);
        }

        return meetings.Count;
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