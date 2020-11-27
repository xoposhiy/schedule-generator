using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using ScheduleLib;
using GoogleSheetsRepository;


namespace Domain.Conversions
{
    public static class SheetToRequestionConverter
    {
        public static List<Requisition> ConvertToRequestions(GSRepository repo, string requestionSheetName, string learningPlanSheetName)
        {
            var PlanData = ReadRowsUsingBoundary(repo, learningPlanSheetName, (1, 0), 6);
            var planItemsAndLocations = ParseLearningPlanItems(PlanData);


            var RequestionData = ReadRowsUsingBoundary(repo, requestionSheetName, (1, 0), 7);

            var requirments = ParseRequirments(RequestionData, planItemsAndLocations);
            return requirments;
        }

        private static List<List<string>> ReadRowsUsingBoundary(GSRepository repo, string SheetName, (int row, int col) start, int width)
        {
            var sheetId = repo.CurrentSheetInfo.Sheets.Keys.ToList().IndexOf(SheetName);

            var ActualRowCount = repo.CurrentSheetInfo.spreadsheet.Sheets[sheetId].Properties.GridProperties.RowCount;
            var RowCountToRead = Math.Min((int)ActualRowCount, 300);
            var testData = repo.ReadCellRange(SheetName, start, (RowCountToRead + 1, width - 1));
            var RowsWithDataCount = testData.Count;
            var dotBoundary = Enumerable.Repeat(new List<string>() { "." }, RowsWithDataCount).ToList();
            repo.ModifySpreadSheet(SheetName).WriteRange((start.row, width), dotBoundary).Execute();
            var PlanData = repo.ReadCellRange(SheetName, start, (RowsWithDataCount, width));
            return PlanData;
        }

        private static List<(LearningPlanItem, string)> ParseLearningPlanItems(List<List<string>> sheetData)
        {
            var MeetingTypeDict = new Dictionary<string, MeetingType>() {
                { "Лекция", MeetingType.Lecture },
                { "КомпПрактика", MeetingType.ComputerLab },
                { "Семинар", MeetingType.Seminar },
                { "Онлайн", MeetingType.Online }
            };

            var GroupSizeDict = new Dictionary<string, GroupSize>() {
                { "в половинках", GroupSize.HalfGroup }
            };

            var learningPlanItems = new List<(LearningPlanItem, string)>();
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
                var discipline = new Discipline(disciplineRow);
                var meetingType = MeetingTypeDict[meetingTypeRow];
                var groupSize = GroupSizeDict.ContainsKey(groupSizeRow) ? GroupSizeDict[groupSizeRow] : GroupSize.FullGroup;
                var meetingCountPerWeek = double.Parse(meetingCountPerWeekRow);
                foreach (var groupName in groups)
                {
                    // Need to add locaton to Learning Plan Item through constructor
                    var leargningPlanItem = new LearningPlanItem(groupName, discipline, meetingType, groupSize, meetingCountPerWeek);
                    learningPlanItems.Add((leargningPlanItem, locationRow));
                }
            }


            return learningPlanItems;
        }

        private static List<Requisition> ParseRequirments(List<List<string>> sheetData, List<(LearningPlanItem, string)> learningPlanItemsLocation)
        {
            var requestions = new List<Requisition>();
            foreach (var requestionRow in sheetData)
            {
                if (requestionRow.Count == 0)
                {
                    continue;
                }
                var teacherName = requestionRow[0];
                var disciplineName = requestionRow[1];
                var meetingType = requestionRow[2];
                var repetitionCountRaw = requestionRow[3];
                var groupPriorities = requestionRow[4];
                var meetingTimesRaw = requestionRow[5];
                var weekType = requestionRow[6];

                var teacher = new Teacher(teacherName);

                var groupRequisitions = groupPriorities
                    .Split("\n")
                    .Select(p => new GroupRequisition(p))
                    .ToArray();
                // Should use meetingType
                var meetingTimes = ParseMeetingTime(meetingTimesRaw);
                var meetingTimesArray = meetingTimes?.ToArray();
                var repetitionCount = repetitionCountRaw.Length != 0 ? int.Parse(repetitionCountRaw) : 1;
                var learningPlanItems = learningPlanItemsLocation.Select(t => t.Item1).ToArray();
                var learingPlan = new LearningPlan(learningPlanItems);

                //var requestion = new Requisition(learingPlan, groupRequisitions, null, repetitionCount, meetingTimesArray, teacher);

                // There is a mistake: How to specify LearningPlanItems properly. Next Line is NOT correct.
                var requestion = new Requisition(learingPlan.Items.First(), groupRequisitions, null, repetitionCount, meetingTimesArray, teacher);
                requestions.Add(requestion);
            }

            return requestions;
        }

        private static List<MeetingTime> ParseMeetingTime(string rawMeetingTime)
        {
            var weekDaysDict = new Dictionary<string, DayOfWeek>() {
                { "пн", DayOfWeek.Monday },
                { "вт", DayOfWeek.Tuesday },
                { "ср", DayOfWeek.Wednesday },
                { "чт", DayOfWeek.Thursday },
                { "пт", DayOfWeek.Friday },
                { "сб", DayOfWeek.Saturday },
                { "вс", DayOfWeek.Sunday },
            };

            var weekDaysStrList = weekDaysDict.Keys.ToList();
            var weekDaysList = new List<DayOfWeek> {
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
            };
            var pattern = @"(?:(?:((?:пн|вт|ср|чт|пт|сб|вс)\s?-\s?(?:пн|вт|ср|чт|пт|сб|вс))|(пн|вт|ср|чт|пт|сб|вс)),?\s?)*\s?(?:(?:(\d\s?-\s?\d)|(\d))\sпара)?";
            var compiledPattern = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

            var meetingTimes = new List<MeetingTime>();

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
                    for (int i = posStart; i < posStart + posEnd; i++)
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
                    for (int i = posStart; i < posStart + posEnd; i++)
                    {
                        currIndexes.Add(i);
                    }
                }

                foreach (Capture indexStr in indexes)
                {
                    var index = int.Parse(indexStr.Value);
                    currIndexes.Add(index);
                }


                foreach (var day in currWeekDays)
                {
                    foreach (var index in currIndexes)
                    {
                        meetingTimes.Add(new MeetingTime(day, index));
                    }
                }
            }

            return meetingTimes;
        }
    }
}
