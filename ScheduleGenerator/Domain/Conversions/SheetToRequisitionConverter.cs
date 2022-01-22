using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Domain.Enums;
using Domain.MeetingsParts;
using Infrastructure.GoogleSheetsRepository;
using Infrastructure.SheetPatterns;
using static Domain.DomainExtensions;

namespace Domain.Conversions
{
    public static class SheetToRequisitionConverter
    {
        public static readonly Dictionary<string, DayOfWeek> WeekDaysDict = new()
        {
            {"пн", DayOfWeek.Monday},
            {"вт", DayOfWeek.Tuesday},
            {"ср", DayOfWeek.Wednesday},
            {"чт", DayOfWeek.Thursday},
            {"пт", DayOfWeek.Friday},
            {"сб", DayOfWeek.Saturday}
        };

        public static readonly Dictionary<string, Location> StringToLocation = new()
        {
            {"Тургенева 4", Location.MathMeh},
            {"Физра", Location.Pe},
            {"Контур", Location.Kontur},
            {"Онлайн", Location.Online}
        };

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

        private static Location GetMeetingLocation(string rowMeetingLocation)
        {
            return StringToLocation[rowMeetingLocation];
        }

        public static (List<RequisitionItem>, LearningPlan, List<RoomRequisition>)
            ConvertToRequisitions(GsRepository repo,
                string requisitionSheetName, string learningPlanSheetName, string classroomsSheetName)
        {
            var planData = SheetTableReader.ReadRowsFromSheet(repo, learningPlanSheetName, 1, 0, 11);
            var learningPlanItems = ParseLearningPlanItems(planData).ToArray();
            var learningPlan = new LearningPlan(learningPlanItems);
            var requisitionData = SheetTableReader.ReadRowsFromSheet(repo, requisitionSheetName, 1, 0, 7);
            var requisitions = ParseRequisitions(requisitionData, learningPlan);
            var classroomsData = SheetTableReader.ReadRowsFromSheet(repo, classroomsSheetName, 1, 0, 5);
            var classrooms = ParseClassrooms(classroomsData);
            return (requisitions, learningPlan, classrooms);
        }

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
            int.TryParse(row[8], out var priority);
            var isHard = !string.IsNullOrEmpty(row[9]);
            var unwantedDisciplines = row[10].Split(',')
                .Select(s => new Discipline(s))
                .ToHashSet();
            var discipline = new Discipline(disciplineRow);

            var meetingType = GetMeetingType(meetingTypeRow);
            var groupSize = GetGroupSize(groupSizeRow);
            var meetingCountPerWeek = double.Parse(meetingCountPerWeekRow, CultureInfo.InvariantCulture);
            return new(groupsRow, discipline, meetingType, groupSize, meetingCountPerWeek, locationRow,
                unwantedDisciplines, connectAfter,
                sameTeacherWith, priority, isHard);
        }

        private static RoomSpec[] ParseLocationSpec(string rowLocationSpec)
        {
            return string.IsNullOrWhiteSpace(rowLocationSpec)
                ? Array.Empty<RoomSpec>()
                : rowLocationSpec.Split(',')
                    .Select(mgs => mgs.Trim())
                    .Select(GetRoomSpec).ToArray();
        }

        private static List<RoomRequisition> ParseClassrooms(IEnumerable<List<string>> sheetData)
        {
            var list = new List<RoomRequisition>();
            foreach (var (room, specs, lockedTimes) in sheetData.Select(ParseClassroom))
                list.Add(new(room, specs, lockedTimes));
            return list;
        }

        private static (string room, List<RoomSpec> specs, HashSet<MeetingTime> lockedTimes) ParseClassroom(
            List<string> row)
        {
            var number = row[0];
            var specs = new List<RoomSpec>();
            if (!string.IsNullOrWhiteSpace(row[1]))
                specs.Add(RoomSpec.Computer);
            if (!string.IsNullOrWhiteSpace(row[2]))
                specs.Add(RoomSpec.Projector);
            if (!string.IsNullOrWhiteSpace(row[3]))
                specs.Add(GetRoomSpec(row[3]));
            var lockedTimes = ParseRoomsTimeRequisitions(row[4]);
            return (number, specs, lockedTimes);
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
                    var location = GetMeetingLocation(requisitionRow[7]);

                    var teacher = new Teacher(teacherName);

                    var groupRequisitions =
                        ParseGroupRequisitions(groupPriorities);
                    var groupSets = groupRequisitions
                        .SelectMany(s => s.GroupsChoices)
                        .SelectMany(c => c.Groups)
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
                        meetingTimeRequisitionArray, teacher, location, weekType);
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
            var groupPriorityLines = rawGroupRequisitions.Split('\n')
                .Select(g => g.Trim())
                .Where(x => !string.IsNullOrEmpty(x));

            var groupRequisitions = new List<GroupRequisition>();

            foreach (var priorityLine in groupPriorityLines)
            {
                var groupChoices = priorityLine.Split(',')
                    .Select(mgs => mgs.Trim())
                    .Select(CreateGroupChoices)
                    .ToArray();
                groupRequisitions.Add(new(groupChoices));
            }

            return groupRequisitions;
        }

        private static GroupsChoice CreateGroupChoices(string meetingGroupString)
        {
            var meetingGroups = new List<MeetingGroup>();
            var meetingGroupStringSplit = meetingGroupString.Split("+").Select(x => x.Trim());
            foreach (var singleMeetingGroup in meetingGroupStringSplit)
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

        public static List<MeetingTimeRequisition> ParseMeetingTimeRequisitions(string rawMeetingTime)
        {
            if (!string.IsNullOrWhiteSpace(rawMeetingTime)) return ParseTimes(rawMeetingTime);
            var meetingTimes = GetAllPossibleMeetingTimes().ToHashSet();
            return new() {new(meetingTimes)};
        }

        private static List<MeetingTimeRequisition> ParseTimes(string rawMeetingTime)
        {
            var meetingTimeRequisitions = new List<MeetingTimeRequisition>();

            var records = rawMeetingTime.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var record in records)
            {
                var meetingTimes = new HashSet<MeetingTime>();

                var blocks = record.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var block in blocks)
                {
                    var parts = block.Replace(" ", "").Split(':');

                    var days = GetDays(parts[0]);
                    var slots = GetSlots(parts[1]);

                    foreach (var day in days)
                    foreach (var slot in slots)
                        meetingTimes.Add(new(day, slot));
                }

                meetingTimeRequisitions.Add(new(meetingTimes));
            }

            return meetingTimeRequisitions;
        }

        private static HashSet<MeetingTime> ParseRoomsTimeRequisitions(string rawMeetingTime)
        {
            return string.IsNullOrWhiteSpace(rawMeetingTime)
                ? new()
                : ParseTimes(rawMeetingTime).SelectMany(e => e.MeetingTimeChoices).ToHashSet();
        }

        private static List<DayOfWeek> GetDays(string dayString)
        {
            var days = new List<DayOfWeek>();
            var dayReqs = dayString.Split(',');
            foreach (var req in dayReqs)
            {
                var tmp = req.Split('-');
                var firstDay = WeekDaysDict[tmp[0]];
                var lastDay = WeekDaysDict[tmp[^1]];
                for (var day = firstDay; day <= lastDay; day++)
                    days.Add(day);
            }

            return days;
        }

        private static List<int> GetSlots(string slotString)
        {
            var slots = new List<int>();
            var slotReqs = slotString.Split(',');
            foreach (var req in slotReqs)
            {
                //пн-пт: 3,4 пара
                var tmp = req.Split('-');
                var firstSlot = int.Parse(tmp[0][0].ToString());
                var lastSlot = tmp.Length == 1 ? firstSlot : int.Parse(tmp[1][0].ToString());
                if (firstSlot < 1 || lastSlot > 6)
                    throw new FormatException("meeting slots must be between 1 and 6");
                for (var slot = firstSlot; slot <= lastSlot; slot++)
                    slots.Add(slot);
            }

            return slots;
        }
    }
}