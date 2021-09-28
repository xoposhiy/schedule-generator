using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Domain.ScheduleLib;
using Infrastructure.GoogleSheetsRepository;
using Infrastructure.SheetPatterns;

namespace Domain.Conversions
{
    public static class SheetToRequisitionConverter
    {
        private const int MaxIndex = 5;

        private static MeetingType GetMeetingType(string rowMeetingType)
        {
            return rowMeetingType switch
            {
                "Лекция" => MeetingType.Lecture,
                "КомпПрактика" => MeetingType.ComputerLab,
                "Семинар" => MeetingType.Seminar,
                _ => throw new FormatException($"Некорректный тип занятия: {rowMeetingType}")
            };
        }

        private static GroupSize GetGroupSize(string rowGroupSize)
        {
            return rowGroupSize switch
            {
                "в половинках" => GroupSize.HalfGroup,
                _ => GroupSize.FullGroup
            };
        }

        private static readonly Dictionary<string, DayOfWeek> WeekDaysDict = new()
        {
            {"пн", DayOfWeek.Monday},
            {"вт", DayOfWeek.Tuesday},
            {"ср", DayOfWeek.Wednesday},
            {"чт", DayOfWeek.Thursday},
            {"пт", DayOfWeek.Friday},
            {"сб", DayOfWeek.Saturday}
        };

        private static DayOfWeek GetDayOfWeek(string rowDayOfWeek)
        {
            return rowDayOfWeek switch
            {
                "пн" => DayOfWeek.Monday,
                "вт" => DayOfWeek.Tuesday,
                "ср" => DayOfWeek.Wednesday,
                "чт" => DayOfWeek.Thursday,
                "пт" => DayOfWeek.Friday,
                "сб" => DayOfWeek.Saturday,
                _ => throw new FormatException($"Некорректный день недели: {rowDayOfWeek}")
            };
        }

        private static GroupPart GetGroupPart(int rowGroupPart)
        {
            return rowGroupPart switch
            {
                1 => GroupPart.Part1,
                2 => GroupPart.Part2,
                _ => throw new FormatException($"Некорректная часть группы: {rowGroupPart}")
            };
        }

        private static WeekType GetWeekType(string rowWeekType)
        {
            return rowWeekType switch
            {
                "любая" => WeekType.OddOrEven,
                "четная" => WeekType.Even,
                "нечетная" => WeekType.Odd,
                _ => throw new FormatException($"Некорректная четность недели: {rowWeekType}")
            };
        }

        private static RoomSpec GetRoomSpec(string rowRoomSpec)
        {
            return rowRoomSpec switch
            {
                "компьютеры" => RoomSpec.Computer,
                "проектор" => RoomSpec.Projector,
                "большая" => RoomSpec.Big,
                "на группу" => RoomSpec.ForGroup,
                _ => throw new FormatException($"Некорректный тип аудитории: {rowRoomSpec}")
            };
        }

        public static (List<RequisitionItem>, LearningPlan, Dictionary<string, List<RoomSpec>>) ConvertToRequisitions(
            GsRepository repo,
            string requisitionSheetName, string learningPlanSheetName, string classroomsSheetName)
        {
            var planData = SheetTableReader.ReadRowsFromSheet(repo, learningPlanSheetName, (1, 0), 8);
            var learningPlanItems = ParseLearningPlanItems(planData).ToArray();
            var learningPlan = new LearningPlan(learningPlanItems);
            var requisitionData = SheetTableReader.ReadRowsFromSheet(repo, requisitionSheetName, (1, 0), 7);
            var requisitions = ParseRequisitions(requisitionData, learningPlan);
            var classroomsData = SheetTableReader.ReadRowsFromSheet(repo, classroomsSheetName, (1, 0), 4);
            var classrooms = ParseClassrooms(classroomsData)
                .ToDictionary(e => e.Item1, e => e.Item2);
            return (requisitions, learningPlan, classrooms);
        }

        //private static List<List<string>> ReadRowsUsingBoundary(GSRepository repo, string SheetName, (int row, int col) start, int width)
        //{
        //    var sheetObj = repo.CurrentSheetInfo.spreadsheet.Sheets.Where(s => s.Properties.Title == SheetName).First();
        //    var actualRowCount = sheetObj.Properties.GridProperties.RowCount;
        //    var rowCountToRead = Math.Min((int)actualRowCount, 300);
        //    var testData = repo.ReadCellRange(SheetName, start, (rowCountToRead + 1, width - 1));
        //    var rowsWithDataCount = testData.Count;
        //    var dotBoundary = Enumerable.Repeat(new List<string>() { "." }, rowsWithDataCount).ToList();
        //    repo.ModifySpreadSheet(SheetName).WriteRange((start.row, width), dotBoundary).Execute();
        //    var sheetData = repo.ReadCellRange(SheetName, start, (rowsWithDataCount, width));
        //    return sheetData;
        //}

        //private static List<List<string>> ReadRowsFromSheet(GSRepository repo, string SheetName, (int row, int col) start, int width)
        //{
        //    var sheetObj = repo.CurrentSheetInfo.spreadsheet.Sheets.Where(s => s.Properties.Title == SheetName).First();
        //    var actualRowCount = sheetObj.Properties.GridProperties.RowCount;
        //    var rowCountToRead = Math.Min((int)actualRowCount, 300);
        //    var testData = repo.ReadCellRange(SheetName, start, (rowCountToRead + 1, width - 1));
        //    var rowsWithDataCount = testData.Count;
        //    var sheetData = repo.ReadCellRange(SheetName, start, (rowsWithDataCount, width));
        //    foreach (var row in sheetData)
        //    {
        //        for (var i = width - row.Count; i > 0; i--)
        //        {
        //            row.Add("");
        //        }
        //    }
        //    return sheetData;
        //}

        private static IEnumerable<LearningPlanItem> ParseLearningPlanItems(List<List<string>> sheetData)
        {
            return sheetData.Select(ParseLearningPlanItem);
        }

        private static LearningPlanItem ParseLearningPlanItem(List<string> row)
        {
            var groupsRow = row[0];
            var disciplineRow = row[1];
            var meetingTypeRow = row[2];
            var groupSizeRow = row[3];
            var meetingCountPerWeekRow = row[4].Replace(',', '.');
            // Use it with LearningPlanItem
            var locationRow = ParseLocationSpec(row[5]);
            MeetingType? connectAfter = string.IsNullOrWhiteSpace(row[6]) ? null : GetMeetingType(row[6]);
            MeetingType? sameTeacherWith = string.IsNullOrWhiteSpace(row[7]) ? null : GetMeetingType(row[7]);
            var discipline = new Discipline(disciplineRow);

            var meetingType = GetMeetingType(meetingTypeRow);
            var groupSize = GetGroupSize(groupSizeRow);
            var meetingCountPerWeek = double.Parse(meetingCountPerWeekRow, CultureInfo.InvariantCulture);
            return new(groupsRow, discipline, meetingType, groupSize, meetingCountPerWeek, locationRow,
                connectAfter,
                sameTeacherWith);
        }

        private static RoomSpec[] ParseLocationSpec(string rowLocationSpec)
        {
            return string.IsNullOrWhiteSpace(rowLocationSpec)
                ? Array.Empty<RoomSpec>()
                : rowLocationSpec.Split(',')
                    .Select(mgs => mgs.Trim())
                    .Select(GetRoomSpec).ToArray();
        }

        private static IEnumerable<(string, List<RoomSpec>)> ParseClassrooms(List<List<string>> sheetData)
        {
            return sheetData.Select(ParseClassroom);
        }

        private static (string, List<RoomSpec>) ParseClassroom(List<string> row)
        {
            var number = row[0];
            var specs = new List<RoomSpec>();
            if (!string.IsNullOrWhiteSpace(row[1]))
                specs.Add(RoomSpec.Computer);
            if (!string.IsNullOrWhiteSpace(row[2]))
                specs.Add(RoomSpec.Projector);
            if (!string.IsNullOrWhiteSpace(row[3]))
                specs.Add(GetRoomSpec(row[3]));
            return (number, specs);
        }

        private static List<RequisitionItem> ParseRequisitions(List<List<string>> sheetData, LearningPlan learningPlan)
        {
            var requisitions = new List<RequisitionItem>();
            foreach (var requisitionRow in sheetData)
            {
                if (requisitionRow.Count == 0 || requisitionRow.Take(8).All(string.IsNullOrEmpty)) continue;

                try
                {
                    var teacherName = requisitionRow[0];
                    var disciplineName = requisitionRow[1];
                    var meetingTypeStr = requisitionRow[2];
                    var meetingType = GetMeetingType(meetingTypeStr);
                    var repetitionCountRaw = requisitionRow[3];
                    var groupPriorities = requisitionRow[4];
                    var meetingTimesRaw = requisitionRow[5];
                    var weekTypeRaw = requisitionRow[6];
                    var isOnline = requisitionRow[7].Trim().Contains("онлайн");

                    var teacher = new Teacher(teacherName);

                    var groupRequisitions =
                        ParseGroupRequisitions(groupPriorities);
                    var groupSets = groupRequisitions
                        .SelectMany(s => s.GroupsChoices
                            .SelectMany(c => c.Groups))
                        .Select(g => g.GetGroupSet()).ToHashSet();
                    if (groupSets.Count != 1)
                        throw new FormatException($"Некорректное описание приоритета групп: {groupPriorities}");
                    var groupSet = groupSets.Single();
                    var meetingTimeRequisitions = ParseMeetingTimeRequisitions(meetingTimesRaw);
                    var meetingTimeRequisitionArray = meetingTimeRequisitions.ToArray();
                    var repetitionCount = repetitionCountRaw.Length != 0 ? int.Parse(repetitionCountRaw) : 1;
                    var planItem = GetPlanItem(learningPlan, disciplineName, meetingType, groupSet);
                    var weekType = ParseWeekType(weekTypeRaw);
                    var requisition = new RequisitionItem(planItem, groupRequisitions.ToArray(), repetitionCount,
                        meetingTimeRequisitionArray, teacher, weekType, isOnline);
                    requisitions.Add(requisition);
                }
                catch (Exception e)
                {
                    throw new FormatException($"Некорректная строка требований: {string.Join(", ", requisitionRow)}",
                        e);
                }
            }

            return requisitions;
        }

        private static LearningPlanItem GetPlanItem(LearningPlan learningPlan, string disciplineName,
            MeetingType meetingType,
            string groupSet)
        {
            var planItems = learningPlan.Items
                .Where(lpi => lpi.Discipline.Name == disciplineName)
                .Where(lpi => lpi.MeetingType == meetingType)
                .Where(lpi => lpi.GroupSet == groupSet)
                .ToList();
            if (planItems.Count == 0)
                throw new FormatException(
                    $"Требования содержат пару ({disciplineName}, {meetingType}), которой нет в учебном плане");
            if (planItems.Count > 1)
                throw new FormatException(
                    $"В учебном плане более одной подходящей пары ({disciplineName}, {meetingType})");

            var planItemAndLocation = planItems.Single();
            return planItemAndLocation;
        }

        private static WeekType ParseWeekType(string? weekTypeRaw)
        {
            return string.IsNullOrEmpty(weekTypeRaw) ? WeekType.All : GetWeekType(weekTypeRaw);
        }

        public static List<GroupRequisition> ParseGroupRequisitions(string rawGroupRequisitions)
        {
            var groupPriorityLines = rawGroupRequisitions.Split('\n').Where(x => !string.IsNullOrEmpty(x.Trim()));
            var groupRequisitions = new List<GroupRequisition>();

            foreach (var priorityLine in groupPriorityLines)
            {
                var groupChoices = new List<GroupsChoice>();
                var meetingGroupsStrings = priorityLine.Split(',').Select(mgs => mgs.Trim());
                foreach (var meetingGroupsString in meetingGroupsStrings)
                {
                    var groupChoice = CreateGroupChoices(meetingGroupsString);
                    groupChoices.Add(groupChoice);
                }

                groupRequisitions.Add(new(groupChoices.ToArray()));
            }

            return groupRequisitions;
        }

        private static GroupsChoice CreateGroupChoices(string meetingGroupString)
        {
            var meetingGroups = new List<MeetingGroup>();
            var meetingGroupStringSplited = meetingGroupString.Split("+").Select(x => x.Trim());
            foreach (var singleMeetingGroup in meetingGroupStringSplited)
            {
                var groupPart = DetermineGroupPart(singleMeetingGroup);
                var parentGroup = GetParentGroup(singleMeetingGroup);
                meetingGroups.Add(new(parentGroup, groupPart));
            }

            return new(meetingGroups.ToArray());
        }

        private static GroupPart DetermineGroupPart(string group)
        {
            var groupPartRegex = new Regex(@"-\s?(\d)$");
            var partMatch = groupPartRegex.Match(group);
            var groupPart = GroupPart.FullGroup;
            if (partMatch.Success)
            {
                var groupPartString = partMatch.Groups[1].Value;
                var groupPartNum = int.Parse(groupPartString);
                groupPart = GetGroupPart(groupPartNum);
            }

            return groupPart;
        }

        private static string GetParentGroup(string group)
        {
            var groupPartRegex = new Regex(@"(\D)*-(\d)*");
            var partMatch = groupPartRegex.Match(group);
            return partMatch.Groups[0].Value;
        }

        // ReSharper disable once UnusedMember.Local
        private static HashSet<string> FindAllMatchingGroups(string group, HashSet<string> groups, bool isLecture)
        {
            var regexedString = group.Replace(" ", "").Replace("-", @"\s?-\s?").Replace("*", @"(?:\d+)");
            regexedString += "$";
            var refex = new Regex(regexedString);
            var matchedGroups = new HashSet<string>();
            foreach (var suspectGroup in groups)
            {
                var isMatch = refex.IsMatch(suspectGroup);
                if (isMatch)
                {
                    if (!isLecture)
                    {
                        matchedGroups.Add($"{suspectGroup}-1");
                        matchedGroups.Add($"{suspectGroup}-2");
                    }
                    else
                    {
                        matchedGroups.Add(suspectGroup);
                    }
                }
            }

            return matchedGroups;
        }

        public static List<MeetingTimeRequisition> ParseMeetingTimeRequisitions(string rawMeetingTime)
        {
            var weekDaysStrList = WeekDaysDict.Keys.ToList();
            var meetingTimeRequisitions = new List<MeetingTimeRequisition>();

            if (string.IsNullOrWhiteSpace(rawMeetingTime))
            {
                var meetingTimes = new List<MeetingTime>();
                foreach (var day in WeekDaysDict.Values)
                    for (var index = 1; index < MaxIndex + 1; index++)
                        meetingTimes.Add(new(day, index));
                var meetingTimeRequisition = new MeetingTimeRequisition(meetingTimes.ToArray());
                meetingTimeRequisitions.Add(meetingTimeRequisition);
                return meetingTimeRequisitions;
            }

            var weekDaysList = new List<DayOfWeek>
            {
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
            };
            var pattern =
                @"(?:(?:((?:пн|вт|ср|чт|пт|сб)\s?-\s?(?:пн|вт|ср|чт|пт|сб))|(пн|вт|ср|чт|пт|сб)),?\s?)*\s?(?:(?:(\d\s?-\s?\d)|(\d))\sпара)?";
            var compiledPattern = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);


            var records = rawMeetingTime.Split('\n');

            foreach (var record in records)
            {
                var matches = compiledPattern.Matches(record);
                var groups = matches[0].Groups;
                var dayRanges = groups[1].Captures;
                var days = groups[2].Captures;
                var indexRanges = groups[3].Captures;
                var indexes = groups[4].Captures;

                var currWeekDays = new List<DayOfWeek>();

                foreach (Capture part in dayRanges)
                {
                    var rangeParts = part.Value.Split('-');
                    var posStart = weekDaysStrList.IndexOf(rangeParts[0]);
                    var posEnd = weekDaysStrList.IndexOf(rangeParts[1]);
                    for (var i = posStart; i < posEnd + 1; i++) currWeekDays.Add(weekDaysList[i]);
                }

                foreach (Capture dayStr in days) currWeekDays.Add(GetDayOfWeek(dayStr.Value));

                var currIndexes = new List<int>();
                foreach (Capture part in indexRanges)
                {
                    var rangeParts = part.Value.Split('-');
                    var posStart = int.Parse(rangeParts[0]);
                    var posEnd = int.Parse(rangeParts[1]);
                    for (var i = posStart; i < posEnd + 1; i++) currIndexes.Add(i);
                }

                foreach (Capture indexStr in indexes)
                {
                    var index = int.Parse(indexStr.Value);
                    currIndexes.Add(index);
                }

                if (currWeekDays.Count == 0) currWeekDays = WeekDaysDict.Values.ToList();

                if (currIndexes.Count == 0) currIndexes.AddRange(new[] {1, 2, 3, 4, 5, 6});

                var meetingTimes = new List<MeetingTime>();
                foreach (var day in currWeekDays)
                foreach (var index in currIndexes)
                    meetingTimes.Add(new(day, index));

                var meetingTimeRequisition = new MeetingTimeRequisition(meetingTimes.ToArray());
                meetingTimeRequisitions.Add(meetingTimeRequisition);
            }

            return meetingTimeRequisitions;
        }
    }
}