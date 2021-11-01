using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Enums;
using Google.Apis.Sheets.v4.Data;
using Infrastructure.GoogleSheetsRepository;
using static Infrastructure.Extensions;

namespace Domain.Conversions
{
    public static class ScheduleSpreadsheetConverter
    {
        private const int TimeBarRowOffset = 2;
        private const int TimeBarColumnOffset = 0;
        private const int HeadersColumnOffset = 2;
        private const int HeadersRowOffset = 0;

        private const int WeekTypesCount = 2;
        private const int SubGroupsCount = 2;

        private static readonly string[] WeekDays = {"ПН", "ВТ", "СР", "ЧТ", "ПТ", "СБ"};
        private static readonly int WeekDayCount = WeekDays.Length;

        private static readonly string[] ClassStarts =
        {
            "I 9:00", "II 10:40", "III 12:50",
            "IV 14:30", "V 16:40", "VI 17:50"
        };

        private static readonly string[] PeClassStarts =
        {
            "8:30 - 10:00", "10:15 - 11:45", "12:00 - 13:30",
            "14:15 - 15:45", "16:00 - 15:30", "слишком поздно"
        };

        private static readonly int StartsCount = ClassStarts.Length;

        private static readonly Color BackgroundColor = new() {Blue = 15 / 16f, Green = 15 / 16f, Red = 15 / 16f};
        private static readonly Color OnlineColor = new() {Blue = 1, Red = 15 / 16f, Green = 15 / 16f};

        public static void Build(IReadonlySchedule schedule, GsRepository repository, string sheetName)
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
            var modifier = repository.ModifySpreadSheet(sheetName);

            modifier.BuildSchedulePattern(groupNames);

            FillScheduleData(modifier, meetingSet, groupNames);
        }

        private static void BuildSchedulePattern(this SheetModifier modifier, List<string> groups)
        {
            modifier.ColorField(groups.Count)
                .BuildTimeBar()
                .BuildGroupHeaders(groups)
                .Execute();
        }

        private static SheetModifier ColorField(this SheetModifier modifier, int groupsCount)
        {
            var height = WeekDayCount * StartsCount * WeekTypesCount;
            var width = groupsCount * SubGroupsCount;
            return modifier.ColorizeRange(TimeBarRowOffset, HeadersColumnOffset, height, width, BackgroundColor);
        }

        private static SheetModifier BuildTimeBar(this SheetModifier modifier)
        {
            var height = StartsCount * WeekTypesCount;
            var rowStart = TimeBarRowOffset;
            foreach (var weekDay in WeekDays.Select(HeaderCellData))
            {
                modifier
                    .WriteRange(rowStart, TimeBarColumnOffset, new() {new() {weekDay}})
                    .AddBorders(rowStart, TimeBarColumnOffset)
                    .MergeCell(rowStart, TimeBarColumnOffset, height, 1);
                foreach (var classStart in ClassStarts.Select(HeaderCellData))
                {
                    modifier
                        .WriteRange(rowStart, TimeBarColumnOffset + 1, new() {new() {classStart}})
                        .AddBorders(rowStart, TimeBarColumnOffset + 1)
                        .MergeCell(rowStart, TimeBarColumnOffset + 1, WeekTypesCount, 1);
                    rowStart += WeekTypesCount;
                }
            }

            return modifier;
        }

        private static SheetModifier BuildGroupHeaders(this SheetModifier modifier, List<string> groups)
        {
            var startColumn = HeadersColumnOffset;
            foreach (var group in groups)
            {
                var subGroupsRow = new List<List<CellData>>()
                    {new() {HeaderCellData(group + "-1"), HeaderCellData(group + "-2")}};
                modifier
                    .WriteRange(HeadersRowOffset, startColumn, new() {new() {HeaderCellData(group)}})
                    .AddBorders(HeadersRowOffset, startColumn)
                    .MergeCell(HeadersRowOffset, startColumn, 1, SubGroupsCount)
                    .WriteRange(HeadersRowOffset + 1, startColumn, subGroupsRow)
                    .AddBorders(HeadersRowOffset + 1, startColumn)
                    .AddBorders(HeadersRowOffset + 1, startColumn + 1);
                startColumn += SubGroupsCount;
            }

            return modifier;
        }

        private static void FillScheduleData(this SheetModifier modifier, HashSet<Meeting> meetings,
            List<string> groups)
        {
            var groupIndexDict = groups
                .Select((g, i) => (g, i))
                .ToDictionary(gi => gi.g, gi => gi.i);

            foreach (var meeting in meetings) WriteMeeting(meeting, groupIndexDict, modifier);
            modifier.Execute();
        }

        private static string MeetingToString(Meeting meeting)
        {
            // TODO krutovsky: create string more careful
            if (meeting.Location == Location.PE)
            {
                var timePeriod = PeClassStarts[meeting.MeetingTime!.TimeSlot - 1];
                return $"ПРИКЛАДНАЯ ФИЗИЧЕСКАЯ КУЛЬТУРА c {timePeriod}";
            }

            var classroom = FillLocation(meeting);
            if (string.IsNullOrEmpty(classroom)) return meeting.Discipline.Name;

            return $"{meeting.Discipline}, " +
                   $"{classroom}, " +
                   $"{meeting.Teacher.Name}"
                // + $", {meeting.MeetingType}"
                ;
        }

        private static CellData MeetingCellData(Meeting meeting)
        {
            var value = MeetingToString(meeting);
            var cellData = CommonCellData(value);
            if (meeting.Location == Location.Online)
                cellData.UserEnteredFormat.BackgroundColor = OnlineColor;

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

        private static readonly Dictionary<DayOfWeek, int> WeekDayToIntDict = new()
        {
            {DayOfWeek.Monday, 0},
            {DayOfWeek.Tuesday, 1},
            {DayOfWeek.Wednesday, 2},
            {DayOfWeek.Thursday, 3},
            {DayOfWeek.Friday, 4},
            {DayOfWeek.Saturday, 5}
            // { DayOfWeek.Sunday, 6}
        };

        private static void WriteMeeting(Meeting meeting, Dictionary<string, int> groupIndexDict,
            SheetModifier modifier)
        {
            var data = MeetingCellData(meeting);
            var payload = new List<List<CellData>> {new() {data}};

            var startRow = GetStartRow(meeting);
            var height = (int) (meeting.Weight * WeekTypesCount);

            var groups = meeting.Groups!.OrderBy(m => m.GroupName).ToList();
            var firstMeetingPos = groupIndexDict[groups[0].GroupName];
            for (var i = 0; i < groups.Count; i++)
            {
                var meetingPos = groupIndexDict[groups[i].GroupName];
                if (i != groups.Count - 1 && groupIndexDict[groups[i + 1].GroupName] - meetingPos == 1) continue;

                var groupPart = groups[i].GroupPart;
                var startColumn = GetStartColumn(firstMeetingPos, groupPart);
                var width = GetMeetingWidth(firstMeetingPos, meetingPos, groupPart);

                modifier
                    .WriteRange(startRow, startColumn, payload)
                    .AddBorders(startRow, startColumn, height, width);

                if (height > 1 || width > 1)
                    modifier.MergeCell(startRow, startColumn, height, width);
                if (i != groups.Count - 1)
                    firstMeetingPos = groupIndexDict[groups[i + 1].GroupName] + 1;
            }
        }

        private static int GetStartRow(Meeting meeting)
        {
            var rowNumOff = WeekDayToIntDict[meeting.MeetingTime!.Day] * StartsCount * WeekTypesCount;
            var startRow = (meeting.MeetingTime.TimeSlot - 1) * WeekTypesCount + rowNumOff;
            var weekOffset = meeting.WeekType == WeekType.Even ? 1 : 0;
            return startRow + TimeBarRowOffset + weekOffset;
        }

        private static int GetStartColumn(int firstMeetingPos, GroupPart groupPart)
        {
            var startColumn = firstMeetingPos * SubGroupsCount;
            var groupOffset = groupPart == GroupPart.Part2 ? 1 : 0;
            return startColumn + HeadersColumnOffset + groupOffset;
        }

        private static int GetMeetingWidth(int firstMeetingPos, int meetingPos, GroupPart groupPart)
        {
            if (groupPart != GroupPart.FullGroup) return 1;
            return SubGroupsCount * (meetingPos - firstMeetingPos + 1);
        }
    }
}