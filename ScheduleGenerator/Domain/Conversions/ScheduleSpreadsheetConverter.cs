﻿using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Enums;
using Google.Apis.Sheets.v4.Data;
using Infrastructure.GoogleSheetsRepository;
using static Infrastructure.Extensions;

namespace Domain.Conversions
{
    public class ScheduleSpreadsheetConverter
    {
        private readonly GsRepository repository;
        private readonly string sheetName;
        private const int TimeBarRowOffset = 2;
        private const int TimeBarColumnOffset = 0;
        private const int HeadersColumnOffset = 2;
        private const int HeadersRowOffset = 0;

        private static readonly string[] WeekDays = {"ПН", "ВТ", "СР", "ЧТ", "ПТ", "СБ"};
        private static readonly int WeekDayCount = WeekDays.Length;

        private static readonly string[] ClassStarts =
        {
            "I 9:00", "II 10:40", "III 12:50",
            "IV 14:30", "V 16:40", "VI 17:50"
        };

        private static readonly int StartIndexesCount = ClassStarts.Length;

        public ScheduleSpreadsheetConverter(GsRepository repo, string sheetName)
        {
            repository = repo;
            this.sheetName = sheetName;
        }

        public void Build(IReadonlySchedule schedule)
        {
            var groupNamesSet = new HashSet<string>();
            var meetingSet = new HashSet<Meeting>();
            foreach (var meeting in schedule.GetMeetings())
            {
                foreach (var group in meeting.Groups!) groupNamesSet.Add(group.GroupName);
                meetingSet.Add(meeting);
            }

            Console.WriteLine($"Прокинется дальше: {meetingSet.Count}");
            var groupNames = groupNamesSet.OrderBy(gn => gn).ToList();

            repository.ClearSheet(sheetName);

            BuildSchedulePattern(groupNames);

            FillScheduleData(meetingSet, groupNames);
        }

        private void BuildSchedulePattern(List<string> groups)
        {
            ColorField(groups);
            BuildTimeBar();
            BuildGroupHeaders(groups);
        }

        private void ColorField(List<string> groups)
        {
            var color = new Color {Blue = 15 / 16f, Green = 15 / 16f, Red = 15 / 16f};
            var height = WeekDayCount * StartIndexesCount * 2;
            var width = groups.Count * 2;
            repository
                .ModifySpreadSheet(sheetName)
                .ColorizeRange(TimeBarRowOffset, HeadersColumnOffset, height, width, color)
                .Execute();
        }

        private void BuildTimeBar()
        {
            var modifier = repository
                .ModifySpreadSheet(sheetName);
            var rowStart = TimeBarRowOffset;
            foreach (var weekDay in WeekDays.Select(HeaderCellData))
            {
                modifier
                    .WriteRange(rowStart, TimeBarColumnOffset, new() {new() {weekDay}})
                    .AddBorders(rowStart, TimeBarColumnOffset)
                    .MergeCell(rowStart, TimeBarColumnOffset, 12, 1);
                foreach (var classStart in ClassStarts.Select(HeaderCellData))
                {
                    modifier
                        .WriteRange(rowStart, TimeBarColumnOffset + 1, new() {new() {classStart}})
                        .AddBorders(rowStart, TimeBarColumnOffset + 1)
                        .MergeCell(rowStart, TimeBarColumnOffset + 1, 2, 1);
                    rowStart += 2;
                }
            }

            modifier.Execute();
        }

        private void BuildGroupHeaders(List<string> groups)
        {
            var modifier = repository
                .ModifySpreadSheet(sheetName);
            var startColumn = HeadersColumnOffset;
            foreach (var group in groups)
            {
                modifier
                    .WriteRange(HeadersRowOffset, startColumn, new() {new() {HeaderCellData(group)}})
                    .AddBorders(HeadersRowOffset, startColumn, 1, 2)
                    .MergeCell(HeadersRowOffset, startColumn, 1, 2)
                    .WriteRange(HeadersRowOffset + 1, startColumn,
                        new() {new() {HeaderCellData(group + "-1"), HeaderCellData(group + "-2")}})
                    .AddBorders(HeadersRowOffset + 1, startColumn)
                    .AddBorders(HeadersRowOffset + 1, startColumn + 1);
                startColumn += 2;
            }

            modifier.Execute();
        }

        private void FillScheduleData(HashSet<Meeting> meetings, List<string> groups)
        {
            var groupIndexDict = groups
                .Select((g, i) => (g, i))
                .ToDictionary(gi => gi.g, gi => gi.i);
            var modifier = repository
                .ModifySpreadSheet(sheetName);

            foreach (var meeting in meetings) WriteMeeting(meeting, groupIndexDict, modifier);
            modifier.Execute();
        }

        public static CellData MeetingCellData(Meeting meeting)
        {
            // TODO krutovsky: create data more carefully
            var classroom = FillLocation(meeting);
            var value = $"{meeting.Discipline}, " +
                        $"{classroom}, " +
                        $"{meeting.Teacher.Name}";
            var cellData = CommonCellData(value);
            if (meeting.Location == Location.Online)
                cellData.UserEnteredFormat.BackgroundColor = new() {Blue = 1, Red = 15 / 16f, Green = 15 / 16f};

            return cellData;
        }

        private static string FillLocation(Meeting meeting)
        {
            return meeting.Location switch
            {
                Location.Kontur => "Контур",
                Location.PE => "ФОК",
                Location.Online => "Онлайн",
                Location.MathMeh => meeting.Classroom ?? "",
                _ => throw new FormatException("Неизвестная Локация")
            };
        }

        private static void WriteMeeting(Meeting meeting, Dictionary<string, int> groupIndexDict,
            SheetModifier modifier)
        {
            var horizOffset = 2;
            var vertOffset = 2;

            var weekDayToIntDict = new Dictionary<DayOfWeek, int>
            {
                {DayOfWeek.Monday, 0},
                {DayOfWeek.Tuesday, 1},
                {DayOfWeek.Wednesday, 2},
                {DayOfWeek.Thursday, 3},
                {DayOfWeek.Friday, 4},
                {DayOfWeek.Saturday, 5}
                // { DayOfWeek.Sunday, 6}
            };

            var groups = meeting.Groups!.OrderBy(m=>m.GroupName).ToList();
            var firstMeetingPos = groupIndexDict[groups[0].GroupName];
            for (int i = 0; i < groups.Count; i++)
            {
                if (i == groups.Count - 1 || groupIndexDict[groups[i + 1].GroupName] -
                    groupIndexDict[groups[i].GroupName] != 1)
                {
                    var data = MeetingCellData(meeting);

                    var rowNumOff = weekDayToIntDict[meeting.MeetingTime!.Day] * 12 + vertOffset;
                    var rowNum = (meeting.MeetingTime.TimeSlotIndex - 1) * 2 + rowNumOff;
                    var rowsInMeeting = 1;
                    if (meeting.WeekType == WeekType.Even) rowNum++;
                    if (meeting.WeekType == WeekType.All) rowsInMeeting = 2;

                    var startColumn = firstMeetingPos * 2 + horizOffset;
                    var columnsInMeeting = 1;
                    if (i == firstMeetingPos)
                    {
                        if (groups[i].GroupPart == GroupPart.Part2) startColumn++;
                        if (groups[i].GroupPart == GroupPart.FullGroup) columnsInMeeting = 2;
                    }
                    else
                    {
                        columnsInMeeting = 2 * (groupIndexDict[groups[i].GroupName] - firstMeetingPos + 1);
                    }
                    
                    modifier
                        .WriteRange(rowNum, startColumn, new() {new() {data}})
                        .AddBorders(rowNum, startColumn, rowsInMeeting, columnsInMeeting);
                    if (rowsInMeeting > 1 || columnsInMeeting > 1)
                        modifier.MergeCell(rowNum, startColumn, rowsInMeeting, columnsInMeeting);
                    if (i != groups.Count - 1)
                        firstMeetingPos = groupIndexDict[groups[i + 1].GroupName] + 1;
                }
            }
        }
    }
}