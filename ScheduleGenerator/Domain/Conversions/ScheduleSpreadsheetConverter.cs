using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Enums;
using Google.Apis.Sheets.v4.Data;
using Infrastructure.GoogleSheetsRepository;
using static Domain.DomainExtensions;
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


        private const int TimeStartColumn = TimeBarColumnOffset + 1;
        private const int SubgroupRowOffset = HeadersRowOffset + 1;

        private static readonly string[] WeekDays = {"ПН", "ВТ", "СР", "ЧТ", "ПТ", "СБ"};
        private static readonly int WeekDayCount = WeekDays.Length;

        private static readonly string[] RomeNumbers =
        {
            "I", "II", "III", "IV", "V", "VI"
        };

        private static readonly string[] MeetingStartTimes =
        {
            "9:00", "10:40", "12:50", "14:30", "16:10", "17:50"
        };

        private static readonly string[] MeetingEndTimes =
        {
            "10:30", "12:10", "14:20", "16:00", "17:40", "19:20"
        };

        private static readonly string[] PeClassStartTimes =
        {
            "8:30", "10:15", "12:00", "14:15", "16:00", "слишком"
        };

        private static readonly string[] PeClassEndTimes =
        {
            "10:00", "11:45", "13:30", "15:45", "17:30", "поздно"
        };

        private static readonly int StartsCount = MeetingStartTimes.Length;

        private static readonly Color BackgroundColor = new() {Blue = 15 / 16f, Green = 15 / 16f, Red = 15 / 16f};
        private static readonly Color OnlineColor = new() {Blue = 1, Red = 15 / 16f, Green = 15 / 16f};

        public static void BuildSchedule(IReadonlySchedule schedule, GsRepository repository, string sheetName)
        {
            var meetingSet = schedule.GetMeetings();
            var groupNames = meetingSet
                .SelectMany(m => m.Groups!)
                .Select(g => g.GroupName)
                .Distinct()
                .OrderBy(gn => gn)
                .ToList();

            Console.WriteLine($"Прокинется дальше: {meetingSet.Count}");

            repository.ClearSheet(sheetName);
            var modifier = repository.ModifySpreadSheet(sheetName);

            modifier.BuildSchedulePattern(groupNames);

            modifier.FillScheduleData(meetingSet, groupNames);
        }

        private static void BuildSchedulePattern(this SheetModifier modifier, List<string> groups)
        {
            modifier.ColorField(groups.Count * SubGroupsCount)
                .BuildTimeBar()
                .BuildGroupHeaders(groups)
                .Execute();
        }

        private static SheetModifier ColorField(this SheetModifier modifier, int width)
        {
            var height = WeekDayCount * StartsCount * WeekTypesCount;
            return modifier.ColorizeRange(TimeBarRowOffset, HeadersColumnOffset, height, width, BackgroundColor);
        }

        private static SheetModifier BuildTimeBar(this SheetModifier modifier)
        {
            var classStarts = RomeNumbers.Select((n, i) => $"{n} {MeetingStartTimes[i]}").ToList();
            var height = StartsCount * WeekTypesCount;
            var rowStart = TimeBarRowOffset;

            foreach (var weekDay in WeekDays.Select(HeaderCellData))
            {
                modifier
                    .WriteRange(rowStart, TimeBarColumnOffset, new() {new() {weekDay}})
                    .AddBorders(rowStart, TimeBarColumnOffset)
                    .MergeCell(rowStart, TimeBarColumnOffset, height, 1);
                foreach (var classStart in classStarts.Select(HeaderCellData))
                {
                    modifier
                        .WriteRange(rowStart, TimeStartColumn, new() {new() {classStart}})
                        .AddBorders(rowStart, TimeStartColumn)
                        .MergeCell(rowStart, TimeStartColumn, WeekTypesCount, 1);
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
                var subGroupsRow = new List<List<CellData>>
                    {new() {HeaderCellData(group + "-1"), HeaderCellData(group + "-2")}};
                modifier
                    .WriteRange(HeadersRowOffset, startColumn, new() {new() {HeaderCellData(group)}})
                    .AddBorders(HeadersRowOffset, startColumn)
                    .MergeCell(HeadersRowOffset, startColumn, 1, SubGroupsCount)
                    .WriteRange(SubgroupRowOffset, startColumn, subGroupsRow)
                    .AddBorders(SubgroupRowOffset, startColumn)
                    .AddBorders(SubgroupRowOffset, startColumn + 1);
                startColumn += SubGroupsCount;
            }

            return modifier;
        }

        private static void FillScheduleData(this SheetModifier modifier, IEnumerable<Meeting> meetings,
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
            var timeSlotIndex = meeting.MeetingTime!.TimeSlot - 1;
            if (meeting.Location == Location.Pe)
            {
                var start = PeClassStartTimes[timeSlotIndex];
                var end = PeClassEndTimes[timeSlotIndex];
                return $"ПРИКЛАДНАЯ ФИЗИЧЕСКАЯ КУЛЬТУРА c {start} - {end}";
            }

            var classroom = FillLocation(meeting);
            if (string.IsNullOrEmpty(classroom)) return meeting.Discipline.Name;

            return $"{meeting.Discipline}, " +
                   $"{classroom}, " +
                   $"{meeting.Teacher.Name}";
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
                Location.Pe => "ФОК",
                Location.Online => "Онлайн",
                Location.MathMeh => meeting.Classroom ?? "",
                _ => throw new FormatException("Неизвестная Локация")
            };
        }

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

        // ReSharper disable once UnusedMember.Global
        public static void WriteRowMeetings(IReadonlySchedule schedule, GsRepository repository, string sheetName)
        {
            // TODO krutovsky: generate header
            var rows = schedule.GetMeetings()
                .OrderBy(m => (m.MeetingTime!.Day, m.MeetingTime!.TimeSlot))
                .Select(GetRowMeetingRaw)
                .ToList();

            repository.ClearCellRange(sheetName, 2, 0, 200, 20);

            repository
                .ModifySpreadSheet(sheetName)
                // .ClearAll()
                .WriteRange(2, 0, rows)
                .Execute();
        }

        private static List<CellData> GetRowMeetingRaw(Meeting meeting)
        {
            var timeSlot = meeting.MeetingTime!.TimeSlot;
            var timeSlotIndex = timeSlot - 1;
            var groups = meeting.Groups!.Select(g => g.GroupName[^1]).Distinct();
            var groupParts = meeting.Groups!.Select(g => GroupPartToString(g.GroupPart)).Distinct();
            var timeStart = meeting.Location == Location.Pe ? PeClassStartTimes : MeetingStartTimes;
            var timeEnd = meeting.Location == Location.Pe ? PeClassEndTimes : MeetingEndTimes;
            return new()
            {
                CommonCellData(DayToString(meeting.MeetingTime!.Day)),
                CommonCellData(timeSlot.ToString()),
                CommonCellData(meeting.Discipline.Name),
                CommonCellData(string.Join(",", groups)),
                CommonCellData(string.Join(",", groupParts)),
                CommonCellData(meeting.Classroom ?? ""),
                CommonCellData(meeting.Teacher.Name),
                CommonCellData(WeekToString(meeting.WeekType)),
                CommonCellData(timeStart[timeSlotIndex]),
                CommonCellData(timeEnd[timeSlotIndex]),
                CommonBoolCellData(),
                CommonBoolCellData(),
                CommonBoolCellData()
            };
        }

        private static string DayToString(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "Понедельник",
                DayOfWeek.Tuesday => "Вторник",
                DayOfWeek.Wednesday => "Среда",
                DayOfWeek.Thursday => "Четверг",
                DayOfWeek.Friday => "Пятница",
                DayOfWeek.Saturday => "Суббота",
                DayOfWeek.Sunday => "Воскресенье",
                _ => throw new ArgumentOutOfRangeException(nameof(dayOfWeek), dayOfWeek, null)
            };
        }

        private static string GroupPartToString(GroupPart groupPart)
        {
            return groupPart switch
            {
                GroupPart.FullGroup => "",
                GroupPart.Part1 => "1",
                GroupPart.Part2 => "2",
                _ => throw new ArgumentOutOfRangeException(nameof(groupPart), groupPart, null)
            };
        }

        private static string WeekToString(WeekType weekType)
        {
            return weekType switch
            {
                WeekType.All => "",
                WeekType.Even => "четная",
                WeekType.Odd => "нечетная",
                _ => throw new ArgumentOutOfRangeException(nameof(weekType), weekType, "Untranslatable")
            };
        }

        // ReSharper disable once UnusedMember.Global
        public static void BuildScheduleByTeacher(IReadonlySchedule schedule, GsRepository repository, string sheetName)
        {
            var meetingSet = schedule.GetMeetings();
            var teachers = meetingSet.Select(m => m.Teacher.Name)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            Console.WriteLine($"Прокинется дальше: {meetingSet.Count}");

            repository.ClearSheet(sheetName);
            var modifier = repository.ModifySpreadSheet(sheetName);

            modifier.BuildSchedulePatternByTeacher(teachers);

            // modifier.FillScheduleData(meetingSet, groupNames);
        }

        private static void BuildSchedulePatternByTeacher(this SheetModifier modifier, List<string> teachers)
        {
            modifier.ColorField(teachers.Count)
                .BuildTimeBar()
                .BuildTeachersHeaders(teachers)
                .Execute();
        }

        private static SheetModifier BuildTeachersHeaders(this SheetModifier modifier, List<string> teachers)
        {
            var startColumn = HeadersColumnOffset;
            foreach (var teacher in teachers.Select(HeaderCellData))
            {
                modifier.WriteRange(HeadersRowOffset, startColumn, new() {new() {teacher}})
                    .AddBorders(HeadersRowOffset, startColumn)
                    .MergeCell(HeadersRowOffset, startColumn, 2, 1);
                startColumn++;
            }

            return modifier;
        }
    }
}