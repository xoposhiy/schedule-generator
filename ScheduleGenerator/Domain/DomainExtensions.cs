using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Domain.Algorithms.Estimators;
using Domain.Enums;
using Domain.MeetingsParts;
using Infrastructure;
using Infrastructure.GoogleSheetsRepository;
using static Domain.Conversions.SheetToRequisitionConverter;
using static Infrastructure.SheetConstants;

namespace Domain
{
    public static class ArrayExtensions
    {
        public static List<Meeting> GetLinkedMeetings(this Meeting meeting)
        {
            var meetings = new List<Meeting> {meeting};
            if (meeting.RequiredAdjacentMeeting != null)
                meetings.Add(meeting.RequiredAdjacentMeeting);
            return meetings;
        }

        private static readonly Dictionary<MeetingGroup[], HashSet<MeetingGroup>> MeetingGroupsCache = new();

        public static HashSet<MeetingGroup> GetGroupParts(this MeetingGroup[] groups)
        {
            if (MeetingGroupsCache.ContainsKey(groups)) return MeetingGroupsCache[groups];

            var cache = new HashSet<MeetingGroup>();
            foreach (var group in groups)
                if (group.GroupPart == GroupPart.FullGroup)
                {
                    cache.Add(group with {GroupPart = GroupPart.Part1});
                    cache.Add(group with {GroupPart = GroupPart.Part2});
                }
                else
                {
                    cache.Add(group);
                }

            return MeetingGroupsCache[groups] = cache;
        }

        public static readonly WeekType[] OddAndEven = {WeekType.Odd, WeekType.Even};
        private static readonly WeekType[] Odd = {WeekType.Odd};
        private static readonly WeekType[] Even = {WeekType.Even};
        public static readonly WeekType[] All = {WeekType.All};

        public static WeekType[] GetWeekTypes(this WeekType weekType)
        {
            return weekType switch
            {
                WeekType.All => OddAndEven,
                WeekType.Odd => Odd,
                WeekType.Even => Even,
                WeekType.OddOrEven => throw new ArgumentException($"{WeekType.OddOrEven} is undetermined to split"),
                _ => throw new ArgumentOutOfRangeException(nameof(weekType), weekType, null)
            };
        }

        public static WeekType[] GetPossibleWeekTypes(this WeekType weekType)
        {
            return weekType == WeekType.OddOrEven ? OddAndEven : GetWeekTypes(weekType);
        }

        public static int GetMeetingsSpacesCount(this Meeting?[] byDay)
        {
            var count = 0;
            var prev = -1;

            for (var i = 1; i < 7; i++)         //index 0 is always null. meetings at 1..6
                if (byDay[i] != null)
                {
                    if (prev != -1) count += i - prev - 1;
                    prev = i;
                }

            return count;
        }


        public static int MeetingsCount(this Meeting?[] byDay)
        {
            var count = 0;
            for (var i = 1; i < 7; i++)     //meetings at 1..6  always null at 0
                if (byDay[i] != null)
                    count++;

            return count;
        }

        public static List<int> MeetingsTimeSlots(this Meeting?[] byDay)
        {
            var res = new List<int>();
            for (var i = 1; i < 7; i++) //meetings at 1..6  always null at 0
                if (byDay[i] != null)
                    res.Add(i);
            return res;
        }
    }

    public static class DictionaryExtensions
    {
        public static void SafeAdd<TKey, TValue>(this Dictionary<TKey, List<TValue>> dict, TKey key, TValue value)
            where TKey : notnull
        {
            if (dict.ContainsKey(key))
                dict[key].Add(value);
            else
                dict.Add(key, new() {value});
        }

        public static bool SafeAdd<TKey1, TValue>(this Dictionary<TKey1, HashSet<TValue>> dictionary, TKey1 key1,
            TValue value)
            where TKey1 : notnull
        {
            if (!dictionary.ContainsKey(key1)) dictionary.Add(key1, new());

            return dictionary[key1].Add(value);
        }

        public static bool SafeAdd<TKey1, TKey2, TKey3, TValue>(
            this Dictionary<TKey1, Dictionary<TKey2, Dictionary<TKey3, HashSet<TValue>>>> dictionary,
            TKey1 key1, TKey2 key2, TKey3 key3, TValue value)
            where TKey1 : notnull
            where TKey2 : notnull
            where TKey3 : notnull
        {
            var byKey2 = dictionary.SafeAddAndReturn(key1, key2, new());

            return byKey2.SafeAdd(key3, value);
        }

        public static void SafeAdd<TKey1>(
            this Dictionary<TKey1, Dictionary<WeekType, Dictionary<DayOfWeek, Meeting?[]>>> dict,
            TKey1 key1, Meeting meeting)
            where TKey1 : notnull
        {
            if (!dict.ContainsKey(key1)) dict[key1] = new();

            var byKey1 = dict[key1];
            var (day, timeSlot) = meeting.MeetingTime!;

            foreach (var weekType in meeting.WeekType.GetWeekTypes())
            {
                var byDay = byKey1.SafeAddAndReturn(weekType, day, new Meeting[7]);
                
                byDay[timeSlot] = meeting;
            }
        }

        public static TValue SafeAddAndReturn<TKey1, TKey2, TValue>(
            this Dictionary<TKey1, Dictionary<TKey2, TValue>> dictionary, TKey1 key1, TKey2 key2, TValue value)
            where TKey1 : notnull
            where TKey2 : notnull
        {
            if (!dictionary.ContainsKey(key1)) dictionary.Add(key1, new());

            var byKey1 = dictionary[key1];

            if (!byKey1.ContainsKey(key2)) byKey1.Add(key2, value);

            return byKey1[key2];
        }

        public static bool TryGetValue<TKey1, TKey2, TValue>(
            this Dictionary<TKey1, Dictionary<TKey2, TValue>> dictionary,
            TKey1 key1, TKey2 key2, [MaybeNullWhen(false)] out TValue value)
            where TKey1 : notnull
            where TKey2 : notnull
        {
            value = default;
            if (!dictionary.TryGetValue(key1, out var byKey1)) return false;
            return byKey1.TryGetValue(key2, out value);
        }

        public static bool TryGetValue<TKey1, TKey2, TKey3, TValue>(
            this Dictionary<TKey1, Dictionary<TKey2, Dictionary<TKey3, TValue>>> dictionary,
            TKey1 key1, TKey2 key2, TKey3 key3, [MaybeNullWhen(false)] out TValue value)
            where TKey1 : notnull
            where TKey2 : notnull
            where TKey3 : notnull
        {
            value = default;
            if (!dictionary.TryGetValue(key1, key2, out var byKey1)) return false;
            return byKey1.TryGetValue(key3, out value);
        }

        public static IEnumerable<Meeting?[]> GetDaysByMeeting<TKey1>(
            this Dictionary<TKey1, Dictionary<WeekType, Dictionary<DayOfWeek, Meeting?[]>>> dictionary,
            TKey1 key1, Meeting meeting)
            where TKey1 : notnull
        {
            var day = meeting.MeetingTime!.Day;
            foreach (var weekType in meeting.WeekType.GetWeekTypes())
            {
                if (!dictionary.TryGetValue(key1, weekType, out var byWeekType)) continue;
                if (!byWeekType.TryGetValue(day, out var byDay)) continue;
                yield return byDay;
            }
        }

        public static bool HasMeetingsAtTime(this IEnumerable<Meeting?[]> days, int timeSlot)
        {
            foreach (var day in days)
                if (day[timeSlot] != null)
                    return true;

            return false;
        }

        public static IEnumerable<(TKey1, WeekType, DayOfWeek, Meeting?[])> Enumerate<TKey1>(
            this Dictionary<TKey1, Dictionary<WeekType, Dictionary<DayOfWeek, Meeting?[]>>> dictionary)
            where TKey1 : notnull
        {
            foreach (var (key1, byGroup) in dictionary)
            foreach (var (weekType, byWeekType) in byGroup)
            foreach (var (day, byDay) in byWeekType)
                yield return (key1, weekType, day, byDay);
        }

        public static void SafeIncrement<TKey>(this Dictionary<TKey, int> dictionary, TKey key)
            where TKey : notnull
        {
            if (!dictionary.ContainsKey(key)) dictionary.Add(key, 0);
            dictionary[key]++;
        }

        public static void SafeIncrement<TKey1, TKey2>(
            this Dictionary<TKey1, Dictionary<TKey2, double>> dict,
            TKey1 key1, TKey2 key2, double value)
            where TKey1 : notnull
            where TKey2 : notnull
        {
            if (!dict.ContainsKey(key1)) dict[key1] = new();

            if (!dict[key1].ContainsKey(key2)) dict[key1][key2] = 0;

            dict[key1][key2] += value;
        }

        public static void SafeDecrement<TKey1, TKey2>(
            this Dictionary<TKey1, Dictionary<TKey2, double>> dict,
            TKey1 key1, TKey2 key2, double value)
            where TKey1 : notnull
            where TKey2 : notnull
        {
            if (!dict.ContainsKey(key1)) throw new FormatException($"Dictionary does not contains key1: {key1}");

            if (!dict[key1].ContainsKey(key2)) throw new FormatException($"Dictionary does not contains key2: {key2}");

            if (dict[key1][key2] == 0)
                return;
            dict[key1][key2] -= value;
        }
    }

    public static class DomainExtensions
    {
        public const int MaxSpaces = 2 * 6 * 4; // weekTypes * daysOfWeek * maxSpaceCount

        public static CombinedEstimator GetDefaultCombinedEstimator()
        {
            var groupsSpacesEstimator = (new StudentsSpacesEstimator(), 1);
            var teacherSpacesEstimator = (new TeacherSpacesEstimator(), 1);
            var meetingsPerDayEstimator = (new MeetingsPerDayEstimator(), 1);
            var teacherUsedDaysEstimator = (new TeacherUsedDaysEstimator(), 1);
            var teacherPriorityEstimator = (new TimePriorityEstimator(), 5);
            var groupPriorityEstimator = (new GroupPriorityEstimator(), 5);
            var dayDurationEstimator = (new DayDurationEstimator(), 1);
            var estimator = new CombinedEstimator(groupsSpacesEstimator,
                meetingsPerDayEstimator, teacherSpacesEstimator, teacherUsedDaysEstimator, teacherPriorityEstimator,
                groupPriorityEstimator, dayDurationEstimator);
            return estimator;
        }

        public static (Requisition, Dictionary<string, List<RoomSpec>>) GetRequisition(
            SheetNamesConfig sheetNamesConfig, GsRepository repo)
        {
            var (requirements, learningPlan, _) = sheetNamesConfig;
            var (requisitions, _, classrooms) = ConvertToRequisitions(
                repo, requirements, learningPlan, ClassroomsSheetName);

            return (new(requisitions.ToArray()), classrooms);
        }

        public static void Link(this Meeting first, Meeting second)
        {
            first.RequiredAdjacentMeeting = second;
            second.RequiredAdjacentMeeting = first;
        }

        public static IEnumerable<MeetingGroup> GetAllGroupParts(this RequisitionItem requisitionItem)
        {
            return requisitionItem.GroupPriorities
                .SelectMany(g => g.GroupsChoices)
                .SelectMany(g => g.GetGroupParts())
                .Distinct();
        }

        private static readonly Dictionary<RequisitionItem, HashSet<MeetingTime>>
            RequisitionToMeetingTimesCache = new();

        public static IReadOnlyCollection<MeetingTime> GetAllMeetingTimes(this RequisitionItem requisitionItem)
        {
            if (RequisitionToMeetingTimesCache.ContainsKey(requisitionItem))
                return RequisitionToMeetingTimesCache[requisitionItem];

            var meetingTimes = requisitionItem.MeetingTimePriorities
                .SelectMany(p => p.MeetingTimeChoices)
                .ToHashSet();

            return RequisitionToMeetingTimesCache[requisitionItem] = meetingTimes;
        }

        public static readonly Dictionary<DayOfWeek, int> WeekDayToIntDict = new()
        {
            {DayOfWeek.Monday, 0},
            {DayOfWeek.Tuesday, 1},
            {DayOfWeek.Wednesday, 2},
            {DayOfWeek.Thursday, 3},
            {DayOfWeek.Friday, 4},
            {DayOfWeek.Saturday, 5}
            // { DayOfWeek.Sunday, 6}
        };

        public static IEnumerable<MeetingTime> GetAllPossibleMeetingTimes()
        {
            foreach (var day in WeekDayToIntDict.Keys.Where(day => day != DayOfWeek.Sunday))
            {
                for (var i = 1; i < 7; i++) yield return new(day, i);
            }
        }

        public static double GetSpacesCountDelta<TKey>(Meeting meetingToAdd, TKey key,
            Dictionary<TKey, Dictionary<WeekType, Dictionary<DayOfWeek, Meeting?[]>>> dictionary) where TKey : notnull
        {
            var weekTypes = meetingToAdd.WeekType.GetWeekTypes();

            var countDelta = 0;
            var dayOfWeek = meetingToAdd.MeetingTime!.Day;

            foreach (var weekType in weekTypes)
            {
                if (!dictionary.TryGetValue(key, weekType, dayOfWeek, out var byDay))
                    continue;
                var before = byDay.GetMeetingsSpacesCount();

                foreach (var linkedMeeting in meetingToAdd.GetLinkedMeetings())
                {
                    var timeSlot = linkedMeeting.MeetingTime!.TimeSlot;
                    if (byDay[timeSlot] != null)
                        throw new ArgumentException("Placing meeting in taken place");

                    byDay[timeSlot] = meetingToAdd;
                }
                
                var after = byDay.GetMeetingsSpacesCount();

                foreach (var linkedMeeting in meetingToAdd.GetLinkedMeetings())
                {
                    var timeSlot = linkedMeeting.MeetingTime!.TimeSlot;
                    byDay[timeSlot] = null;
                }

                countDelta += after - before;
            }

            return countDelta;
        }
    }
}