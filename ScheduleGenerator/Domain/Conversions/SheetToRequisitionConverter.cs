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
        private static Dictionary<string, MeetingType> meetingTypeDict = new Dictionary<string, MeetingType>() {
            { "Лекция", MeetingType.Lecture },
            { "КомпПрактика", MeetingType.ComputerLab },
            { "Семинар", MeetingType.Seminar },
            { "Онлайн", MeetingType.Online }
        };

        private static Dictionary<string, GroupSize> groupSizeDict = new Dictionary<string, GroupSize>() {
            { "в половинках", GroupSize.HalfGroup }
        };

        private static Dictionary<string, DayOfWeek> weekDaysDict = new Dictionary<string, DayOfWeek>() {
            { "пн", DayOfWeek.Monday },
            { "вт", DayOfWeek.Tuesday },
            { "ср", DayOfWeek.Wednesday },
            { "чт", DayOfWeek.Thursday },
            { "пт", DayOfWeek.Friday },
            { "сб", DayOfWeek.Saturday },
            { "вс", DayOfWeek.Sunday },
        };

        private static Dictionary<int, GroupPart> groupPartDict = new Dictionary<int, GroupPart>() {
            { 1, GroupPart.Part1 },
            { 2, GroupPart.Part2 },
            { 3, GroupPart.Part3 }
        };

        private static Dictionary<string, WeekType> weekTypeDict = new Dictionary<string, WeekType>() {
            { "любая", WeekType.Any },
            { "четная", WeekType.Even },
            { "нечетная", WeekType.Odd }
        };

        public static (List<RequisitionItem>, LearningPlan) ConvertToRequisitions(GSRepository repo, string requisitionSheetName, string learningPlanSheetName)
        {
            var PlanData = SheetTableReader.ReadRowsFromSheet(repo, learningPlanSheetName, (1, 0), 6);
            var (planItemsAndLocations, allGroups) = ParseLearningPlanItems(PlanData);
            var learningPlanItems = planItemsAndLocations.Select(x => x.Item1).ToArray();
            var learningPlan = new LearningPlan(learningPlanItems);
            var RequestionData = SheetTableReader.ReadRowsFromSheet(repo, requisitionSheetName, (1, 0), 7);
            var requisitions = ParseRequisitions(RequestionData, planItemsAndLocations, allGroups);
            return (requisitions, learningPlan);
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

        private static (List<(LearningPlanItem, string)>, HashSet<string>) ParseLearningPlanItems(List<List<string>> sheetData)
        {
            var learningPlanItems = new List<(LearningPlanItem, string)>();
            var allGroups = new HashSet<string>();
            foreach (var row in sheetData)
            {
                var groupsRow = row[0];
                var disciplineRow = row[1];
                var meetingTypeRow = row[2];
                var groupSizeRow = row[3];
                var meetingCountPerWeekRow = row[4];
                // Use it with LearningPlanItem
                var locationRow = row[5];

                var groups = groupsRow.Split(',').Select(s => s.Trim()).ToList();
                foreach (var group in groups)
                {
                    allGroups.Add(group);
                }
                var discipline = new Discipline(disciplineRow);
                var meetingType = meetingTypeDict[meetingTypeRow];
                var groupSize = groupSizeDict.ContainsKey(groupSizeRow) ? groupSizeDict[groupSizeRow] : GroupSize.FullGroup;
                var meetingCountPerWeek = double.Parse(meetingCountPerWeekRow, CultureInfo.InvariantCulture);
                foreach (var groupName in groups)
                {
                    // Need to add locaton to Learning Plan Item through constructor
                    var leargningPlanItem = new LearningPlanItem(groupName, discipline, meetingType, groupSize, meetingCountPerWeek);
                    learningPlanItems.Add((leargningPlanItem, locationRow));
                }
            }

            return (learningPlanItems, allGroups);
        }

        private static List<RequisitionItem> ParseRequisitions(List<List<string>> sheetData, List<(LearningPlanItem, string)> learningPlanItemsLocation, HashSet<string> allGroups)
        {
            var requisitions = new List<RequisitionItem>();
            foreach (var requisitionRow in sheetData)
            {
                if (requisitionRow.Count == 0 || requisitionRow.Take(7).All(string.IsNullOrEmpty))
                {
                    continue;
                }

                try
                {
                    var teacherName = requisitionRow[0];
                    var disciplineName = requisitionRow[1];
                    var meetingTypeStr = requisitionRow[2];
                    var meetingType = meetingTypeDict[meetingTypeStr];
                    var repetitionCountRaw = requisitionRow[3];
                    var groupPriorities = requisitionRow[4];
                    var meetingTimesRaw = requisitionRow[5];
                    var weekTypeRaw = requisitionRow[6];

                    var teacher = new Teacher(teacherName);

                    var groupRequisitions =
                        ParseGroupRequisitions(groupPriorities, allGroups, meetingType == MeetingType.Lecture);
                    var meetingTimeRequisitions = ParseMeetingTimeRequisitions(meetingTimesRaw);
                    var meetingTimeRequisitionArray = meetingTimeRequisitions.ToArray();
                    var repetitionCount = repetitionCountRaw.Length != 0 ? int.Parse(repetitionCountRaw) : 1;
                    var planItemAndLocations = learningPlanItemsLocation
                        .Where(lpi => lpi.Item1.Discipline.Name == disciplineName)
                        .Where(lpi => lpi.Item1.MeetingType == meetingType)
                        .ToList();
                    if (planItemAndLocations.Count == 0)
                        throw new FormatException(
                            $"Требования содержат пару ({disciplineName}, {meetingType}), которой нет в учебном плане");
                    // if (planItemAndLocations.Count > 1)
                    //     throw new FormatException(
                    //         $"Учебный план почему-то содержит несколько пар ({disciplineName}, {meetingType})");
                    var planItemAndLocation = planItemAndLocations[0];
                    var weekType = weekTypeRaw.Length == 0 ? WeekType.Any : weekTypeDict[weekTypeRaw];
                    var requisition = new RequisitionItem(planItemAndLocation.Item1, groupRequisitions.ToArray(),
                        planItemAndLocation.Item2, repetitionCount, meetingTimeRequisitionArray, teacher, weekType);
                    requisitions.Add(requisition);

                }
                catch (Exception e)
                {
                    throw new FormatException($"Некорректная строка требований: {string.Join(", ", requisitionRow)}", e);
                }
            }
            return requisitions;
        }

        private static List<GroupRequisition> ParseGroupRequisitions(string rawGroupRequisitions, HashSet<string> allGroups, bool isLecture)
        {
            if (string.IsNullOrWhiteSpace(rawGroupRequisitions))
            {
                rawGroupRequisitions = string.Join(", ", allGroups);
            }
            var groupPriorityLines = rawGroupRequisitions.Split('\n').Where(x => !string.IsNullOrEmpty(x.Trim()));
            var groupRequisitions = new List<GroupRequisition>();

            foreach (var priorityLine in groupPriorityLines)
            {
                var groupChoices = new List<GroupsChoice>();
                var meetingGroupsStrings = priorityLine.Split(',').Select(mgs => mgs.Trim());
                foreach (var meetingGroupsString in meetingGroupsStrings)
                {
                    var groupChoice = CreateGroupChoices(meetingGroupsString, allGroups, isLecture);
                    groupChoices.Add(groupChoice);
                }

                groupRequisitions.Add(new GroupRequisition(groupChoices.ToArray()));
            }

            return groupRequisitions;
        }

        private static GroupsChoice CreateGroupChoices(string meetingGroupString, HashSet<string> allGroups, bool isLecture)
        {
            var meetingGroups = new List<MeetingGroup>();
            var MeetingGroupStringSplited = meetingGroupString.Split("+").Select(x => x.Trim());
            foreach (var singleMeetingGroup in MeetingGroupStringSplited)
            {
                if (singleMeetingGroup.Contains("*"))
                {
                    var possibleGroups = FindAllMatchingGroups(singleMeetingGroup, allGroups, isLecture);
                    foreach (var matchedGroup in possibleGroups)
                    {
                        var currGroupPart = DetermineGroupPart(matchedGroup);
                        var meetingGroup = new MeetingGroup(matchedGroup, currGroupPart);
                        meetingGroups.Add(meetingGroup);
                    }
                    continue;
                }

                var groupPart = DetermineGroupPart(singleMeetingGroup);
                meetingGroups.Add(new MeetingGroup(singleMeetingGroup, groupPart));
            }

            return new GroupsChoice(meetingGroups.ToArray());
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
                groupPart = groupPartDict[groupPartNum];
            }

            return groupPart;
        }

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

        private static List<MeetingTimeRequisition> ParseMeetingTimeRequisitions(string rawMeetingTime)
        {
            var weekDaysStrList = weekDaysDict.Keys.ToList();
            var meetingTimeRequisitions = new List<MeetingTimeRequisition>();

            if (string.IsNullOrWhiteSpace(rawMeetingTime))
            {
                var meetingTimes = new List<MeetingTime>();
                foreach (var day in weekDaysDict.Values)
                {
                    for (var index = 0; index < MaxIndex + 1; index++)
                    {
                        meetingTimes.Add(new MeetingTime(day, index));
                    }
                }
                var meetingTimeRequisition = new MeetingTimeRequisition(meetingTimes.ToArray());
                meetingTimeRequisitions.Add(meetingTimeRequisition);
                return meetingTimeRequisitions;
            }

            var weekDaysList = new List<DayOfWeek> {
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
            };
            var pattern = @"(?:(?:((?:пн|вт|ср|чт|пт|сб|вс)\s?-\s?(?:пн|вт|ср|чт|пт|сб|вс))|(пн|вт|ср|чт|пт|сб|вс)),?\s?)*\s?(?:(?:(\d\s?-\s?\d)|(\d))\sпара)?";
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
                    for (int i = posStart; i < posEnd + 1; i++)
                    {
                        currWeekDays.Add(weekDaysList[i]);
                    }
                }
                foreach (Capture dayStr in days)
                {
                    currWeekDays.Add(weekDaysDict[dayStr.Value]);
                }

                var currIndexes = new List<int>();
                foreach (Capture part in indexRanges)
                {
                    var rangeParts = part.Value.Split('-');
                    var posStart = int.Parse(rangeParts[0]);
                    var posEnd = int.Parse(rangeParts[1]);
                    for (int i = posStart; i < posEnd + 1; i++)
                    {
                        currIndexes.Add(i);
                    }
                }

                foreach (Capture indexStr in indexes)
                {
                    var index = int.Parse(indexStr.Value);
                    currIndexes.Add(index);
                }

                if (currWeekDays.Count == 0)
                {
                    currWeekDays = weekDaysDict.Values.ToList();
                }

                if (currIndexes.Count == 0)
                {
                    currIndexes.AddRange(new[] { 0, 1, 2, 3, 4, 5 });
                }

                var meetingTimes = new List<MeetingTime>();
                foreach (var day in currWeekDays)
                {
                    foreach (var index in currIndexes)
                    {
                        meetingTimes.Add(new MeetingTime(day, index));
                    }
                }

                var meetingTimeRequisition = new MeetingTimeRequisition(meetingTimes.ToArray());
                meetingTimeRequisitions.Add(meetingTimeRequisition);
            }

            return meetingTimeRequisitions;
        }
    }
}
