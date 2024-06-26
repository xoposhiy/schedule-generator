﻿using CommonInfrastructure;
using CommonInfrastructure.GoogleSheetsRepository;
using Google.Apis.Sheets.v4.Data;
using static CommonInfrastructure.Extensions;

namespace Domain2;

public static class Visualizer
{
    private const int RowOffset = 1;
    private const int ColumnOffset = 1;

    public static void DrawSchedule(GsRepository repository, State state, string sheetName)
    {
        var meetings = state.PlacedMeetings;
        var dayDuration = meetings
            .Where(m => m.ShouldBePlaced)
            .Max(m => m.MeetingTime!.TimeSlotIndex + m.Duration);
        var meetingsByDay = meetings
            .Where(m => m.ShouldBePlaced)
            .GroupBy(m => m.MeetingTime!.DayOfWeek)
            .OrderBy(g => g.Key);

        repository.ClearSheet(sheetName);
        using var modifier = repository.ModifySpreadSheet(sheetName);
        var columnOffset = ColumnOffset;
        foreach (var meetingsSet in meetingsByDay)
            columnOffset += modifier.DrawMeetingsPerDay(meetingsSet, columnOffset, dayDuration);

        modifier.BuildTimeSlotsBar(0, RowOffset, 1, 1, dayDuration);
        LogWorstParallelDisciplines(state, modifier);
    }

    public static void LogWorstParallelDisciplines(State state, SheetModifier sheetModifier)
    {
        var errors = new List<(string when, double students, Meeting2 m1, Meeting2 m2)>();
        foreach (var day in state.PlacedMeetings.GroupBy(m => m.MeetingTime.DayOfWeek))
        {
            foreach (var slot in day.GroupBy(d => d.MeetingTime.TimeSlot))
            {
                var when = day.Key + " " + slot.Key;
                foreach (var m1 in slot)
                foreach (var m2 in slot)
                {
                    if (m1.Discipline.Name.CompareTo(m2.Discipline.Name) > 0 && !m1.Ignore && !m2.Ignore)
                    {
                        var commonStudents = state.ProbabilityStorage.GetCommonStudents(m1, m2);
                        if (commonStudents > 0)
                            errors.Add((when, commonStudents, m1, m2));
                    }
                }
            }
        }

        var data = errors
            .OrderByDescending(t => t.students)
            .Select(t => new List<string> { t.students.ToString(), t.when, t.m1.Discipline.Name, t.m2.Discipline.Name }
                .Select(CommonCellData).ToList())
            .Prepend(new List<CellData>(){CommonCellData("Пострадало студентов:")})
            .ToList();
        sheetModifier.WriteRange(21, 0, data)
            .Execute();

    }


    private static int DrawMeetingsPerDay(this SheetModifier modifier, IGrouping<DayOfWeek, Meeting2> meetings,
        int columnOffset, int dayDuration)
    {
        var day = meetings.Key.ToRuString();
        var columns = meetings
            .GroupBy(m => m.Discipline)
            .SelectMany(g => g.GetDisciplineColumn(dayDuration))
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
                var width = GetWidth(columns, i, y);
                var startRow = RowOffset + y;
                var startColumn = columnOffset + i;
                modifier
                    .WriteRange(startRow, startColumn, meetingData)
                    .MergeCell(startRow, startColumn, height, width);
                y += height - 1;
            }
        }

        modifier.Execute();
        return modifier;
    }

    private static int GetWidth(IReadOnlyList<Meeting2?[]> columns, int columnIndex, int timeSlotIndex)
    {
        var column = columns[columnIndex];
        var meeting = column[timeSlotIndex];

        var width = 1;
        for (; columnIndex + width < columns.Count; width++)
        for (var i = 0; i < meeting!.Duration; i++)
            if (columns[columnIndex + width][timeSlotIndex + i] != null)
                return width;

        return width;
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

    public static void UpdateMeetingsData(GsRepository repo, string sheetName, State state) =>
        UpdateMeetingsData(repo, sheetName, state.PlacedMeetings);

    public static void UpdateMeetingsData(GsRepository repo, string sheetName, IEnumerable<Meeting2> meetings)
    {
        var meetingsDataRaw = repo.ReadCellRange(sheetName, 0, 0, 0, Constants.FormattedMeetingsRowWidth);
        var headerRow = meetingsDataRaw?[0];
        if (headerRow == null) return;
        var positions = headerRow!.GetPositions();
        using var modifier = repo.ModifySpreadSheet(sheetName);
        foreach (var meeting in meetings.Where(m => m.ShouldBePlaced))
        {
            var classRoom = CommonCellData(meeting.ClassRoom ?? "");
            var time = CommonCellData(meeting.MeetingTime?.ToString() ?? "alo");
            modifier
                .WriteRange(meeting.Id + 1, positions["ClassRoom"], new() {new() {classRoom}})
                .WriteRange(meeting.Id + 1, positions["Time"], new() {new() {time}});
        }
    }
}