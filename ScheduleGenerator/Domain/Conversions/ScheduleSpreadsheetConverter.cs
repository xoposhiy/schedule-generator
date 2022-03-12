using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Enums;
using Google.Apis.Sheets.v4.Data;
using Infrastructure.GoogleSheetsRepository;
using static Domain.DomainExtensions;
using static Infrastructure.Extensions;
using static Infrastructure.LoggerExtension;

namespace Domain.Conversions
{
    public static class ScheduleSpreadsheetConverter
    {
        private const int TimeBarRowOffset = 1;
        private const int TimeBarColumnOffset = 0;
        private const int HeadersColumnOffset = 2;
        private const int HeadersRowOffset = 0;

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

        private static readonly SheetModifier.BordersWidths ThickBorders = new(0, 2, 0, 2);

        public static void BuildSchedule(IReadonlySchedule schedule, GsRepository repository, string sheetName)
        {
            var meetingSet = schedule.GetMeetings();
            var groupNames = meetingSet
                .SelectMany(m => m.GroupsChoice!.Groups)
                .Where(mg => mg.GroupPart != GroupPart.FullGroup)
                .Select(g => g.ToString())
                .Distinct()
                .OrderBy(gn => gn)
                .ToList();

            var groupNames2 = meetingSet
                .SelectMany(m => m.GroupsChoice!.Groups)
                .Select(g => g.GroupName)
                .Distinct()
                .OrderBy(gn => gn)
                .ToList();

            WriteLog($"Прокинется дальше: {meetingSet.Count}");

            repository.ClearSheet(sheetName);

            using var modifier = repository.ModifySpreadSheet(sheetName);
            modifier.BuildSchedulePattern(groupNames);

            modifier.FillScheduleData(meetingSet, groupNames);

            modifier.BuildThickBorders(groupNames.Count + HeadersColumnOffset);
        }

        private static void BuildSchedulePattern(this SheetModifier modifier, List<string> groups)
        {
            modifier.ColorField(groups.Count)
                .BuildTimeBar()
                .BuildGroupHeaders(groups);
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

        private static SheetModifier BuildThickBorders(this SheetModifier modifier, int width)
        {
            var height = RomeNumbers.Length * WeekTypesCount;
            var currentRow = TimeBarRowOffset + (WeekDayCount - 1) * height;
            for (var i = 0; i < WeekDayCount; i++)
            {
                modifier.AddBorders(currentRow, TimeBarColumnOffset, height, width, ThickBorders);
                currentRow -= height;
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
                    // .MergeCell(HeadersRowOffset, startColumn, 1, SubGroupsCount)
                    // .WriteRange(SubgroupRowOffset, startColumn, subGroupsRow)
                    // .AddBorders(SubgroupRowOffset, startColumn)
                    // .AddBorders(SubgroupRowOffset, startColumn + 1)
                    ;
                startColumn++;
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
            // modifier.Execute();
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

            var groups = meeting.GroupsChoice!.GetGroupParts()
                .Select(g => g.ToString())
                .OrderBy(g => g)
                .ToList();
            var firstMeetingPos = groupIndexDict[groups[0]];
            for (var i = 0; i < groups.Count; i++)
            {
                var meetingPos = groupIndexDict[groups[i]];
                if (i != groups.Count - 1 && groupIndexDict[groups[i + 1]] - meetingPos == 1) continue;

                //var groupPart = groups[i].GroupPart;
                var startColumn = GetStartColumn(firstMeetingPos);
                var width = GetMeetingWidth(firstMeetingPos, meetingPos);

                modifier
                    .WriteRange(startRow, startColumn, payload)
                    .AddBorders(startRow, startColumn, height, width);

                if (height > 1 || width > 1)
                    modifier.MergeCell(startRow, startColumn, height, width);
                if (i != groups.Count - 1)
                    firstMeetingPos = groupIndexDict[groups[i + 1]];
            }
        }

        private static int GetStartRow(Meeting meeting)
        {
            var rowNumOff = WeekDayToIntDict[meeting.MeetingTime!.Day] * StartsCount * WeekTypesCount;
            var startRow = (meeting.MeetingTime.TimeSlot - 1) * WeekTypesCount + rowNumOff;
            var weekOffset = meeting.WeekType == WeekType.Even ? 1 : 0;
            return startRow + TimeBarRowOffset + weekOffset;
        }

        private static int GetStartColumn(int firstMeetingPos)
        {
            var startColumn = firstMeetingPos;
            return startColumn + HeadersColumnOffset;
        }

        private static int GetMeetingWidth(int firstMeetingPos, int meetingPos)
        {
            // if (groupPart != GroupPart.FullGroup) return 1;
            return (meetingPos - firstMeetingPos + 1);
        }

        public static void WriteRowMeetings(IReadonlySchedule schedule, GsRepository repository, string sheetName)
        {
            WriteMeetingsData(schedule, repository, sheetName, GetRowMeetingRaw);
        }

        public static void WriteMeetingRequisition(IReadonlySchedule schedule, GsRepository repository,
            string sheetName)
        {
            WriteMeetingsData(schedule, repository, sheetName, GetRequisitionMeetingRow);
        }

        private static void WriteMeetingsData(IReadonlySchedule schedule, GsRepository repository, string sheetName,
            Func<Meeting, List<CellData>> converter)
        {
            var rows = schedule.GetMeetings()
                .OrderBy(m => (m.MeetingTime!.Day, m.MeetingTime!.TimeSlot))
                .Select(converter)
                .ToList();

            repository.ClearCellRange(sheetName, 2, 0, rows.Count + 2, rows[0].Count);

            using var modifier = repository.ModifySpreadSheet(sheetName);
            modifier.WriteRange(2, 0, rows);
        }

        private static List<CellData> GetRequisitionMeetingRow(Meeting meeting)
        {
            var groups = meeting.GroupsChoice!.Groups.Select(g =>
            {
                var part = g.GroupPart.GroupPartToString();
                return part != "" ? $"{g.GroupName}-{part}" : g.GroupName;
            });

            var day = SheetToRequisitionConverter.WeekDaysDict.First(p => p.Value == meeting.MeetingTime!.Day).Key;
            var location = SheetToRequisitionConverter.StringToLocation.First(p => p.Value == meeting.Location).Key;

            var time = $"{day}: {meeting.MeetingTime!.TimeSlot}";
            return new()
            {
                CommonCellData(meeting.Teacher.Name),
                CommonCellData(meeting.Discipline.Name),
                CommonCellData(meeting.MeetingType.GetMeetingTypeString()),
                CommonCellData("1"),
                CommonCellData(string.Join("+", groups)),
                CommonCellData(time),
                CommonCellData(meeting.WeekType.WeekToString()),
                CommonCellData(location)
            };
        }

        private static string GetMeetingTypeString(this MeetingType meetingType)
        {
            return meetingType switch
            {
                MeetingType.Lecture => "Лекция",
                MeetingType.ComputerLab => "КомпПрактика",
                MeetingType.Seminar => "Семинар",
                _ => throw new FormatException($"Некорректный тип занятия: {meetingType}")
            };
        }

        private static List<CellData> GetRowMeetingRaw(Meeting meeting)
        {
            var timeSlot = meeting.MeetingTime!.TimeSlot;
            var timeSlotIndex = timeSlot - 1;
            var groups = meeting.GroupsChoice!.Groups.Select(g => g.GroupName.Replace("ФИИТ", "ФТ"));
            var groupParts = meeting.GroupsChoice!.Groups.Select(g => GroupPartToString(g.GroupPart)).Distinct();
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
                CommonTimeCellData(DateTime.Now)
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

        private static string GroupPartToString(this GroupPart groupPart)
        {
            return groupPart switch
            {
                GroupPart.FullGroup => "",
                GroupPart.Part1 => "1",
                GroupPart.Part2 => "2",
                _ => throw new ArgumentOutOfRangeException(nameof(groupPart), groupPart, null)
            };
        }

        private static string WeekToString(this WeekType weekType)
        {
            return weekType switch
            {
                WeekType.All => "",
                WeekType.Even => "четная",
                WeekType.Odd => "нечетная",
                _ => throw new ArgumentOutOfRangeException(nameof(weekType), weekType, "Untranslatable")
            };
        }

        public static void BuildScheduleByTeacher(IReadonlySchedule schedule, GsRepository repository, string sheetName)
        {
            var meetingSet = schedule.GetMeetings();
            // var teachers = meetingSet.Select(m => m.Teacher.Name)
            //     .Distinct()
            //     .OrderBy(t => t)
            //     .ToList();

            var teachers = meetingSet
                .OrderBy(t => t.MeetingTime!.Day)
                .Select(t => t.Teacher.Name)
                .Distinct()
                .ToList();

            WriteLog($"Прокинется дальше: {meetingSet.Count}");

            repository.ClearSheet(sheetName);
            using var modifier = repository.ModifySpreadSheet(sheetName);

            modifier.BuildSchedulePatternByTeacher(teachers);

            modifier.FillScheduleDataByTeacher(meetingSet, teachers);

            modifier.BuildThickBorders(teachers.Count + 2);
        }

        private static void BuildSchedulePatternByTeacher(this SheetModifier modifier, List<string> teachers)
        {
            modifier.ColorField(teachers.Count)
                .BuildTimeBar()
                .BuildTeachersHeaders(teachers);
            // .Execute();
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

        private static void FillScheduleDataByTeacher(this SheetModifier modifier, IEnumerable<Meeting> meetings,
            List<string> teachers)
        {
            foreach (var meeting in meetings)
            {
                var data = MeetingCellData(meeting);
                data.UserEnteredValue.StringValue = $"{meeting.Discipline}, {FillLocation(meeting)}";
                var payload = new List<List<CellData>> {new() {data}};

                var startRow = GetStartRow(meeting);
                var height = (int) (meeting.Weight * WeekTypesCount);

                var startColumn = teachers.FindIndex(t => t == meeting.Teacher.Name) + HeadersColumnOffset;

                modifier
                    .WriteRange(startRow, startColumn, payload)
                    .AddBorders(startRow, startColumn, height)
                    .MergeCell(startRow, startColumn, height, 1);
            }
            // modifier.Execute();
        }
    }
}