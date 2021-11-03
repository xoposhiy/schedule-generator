using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Algorithms;
using Domain.Algorithms.Estimators;
using Domain.Algorithms.Solvers;
using Domain.Conversions;
using Domain.Enums;
using Domain.MeetingsParts;
using Infrastructure;
using Infrastructure.GoogleSheetsRepository;
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

        public static IEnumerable<MeetingGroup> GetGroupParts(this MeetingGroup[] groups)
        {
            foreach (var group in groups)
                if (group.GroupPart == GroupPart.FullGroup)
                {
                    yield return group with {GroupPart = GroupPart.Part1};
                    yield return group with {GroupPart = GroupPart.Part2};
                }
                else
                {
                    yield return group;
                }
        }

        public static IEnumerable<WeekType> GetWeekTypes(this WeekType weekType)
        {
            if (weekType == WeekType.OddOrEven)
                throw new ArgumentException($"{WeekType.OddOrEven} is undetermined to split");
            if (weekType == WeekType.All)
            {
                yield return WeekType.Even;
                yield return WeekType.Odd;
            }
            else
            {
                yield return weekType;
            }
        }

        public static int GetMeetingsSpacesCount(this Meeting?[] byDay)
        {
            var count = 0;
            var prev = -1;

            for (var i = 0; i < 7; i++)
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
            for (var i = 0; i < 7; i++)
                if (byDay[i] != null)
                    count++;

            return count;
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
                if (!byKey1.ContainsKey(weekType)) byKey1[weekType] = new();
                var byWeekType = byKey1[weekType];

                if (!byWeekType.ContainsKey(day)) byWeekType[day] = new Meeting[7];
                var byDay = byWeekType[day];

                byDay[timeSlot] = meeting;
            }
        }

        public static IEnumerable<Meeting?[]> GetDaysByMeeting<TKey1>(
            this Dictionary<TKey1, Dictionary<WeekType, Dictionary<DayOfWeek, Meeting?[]>>> dictionary,
            TKey1 key1, Meeting meeting)
            where TKey1 : notnull
        {
            var day = meeting.MeetingTime!.Day;
            foreach (var weekType in meeting.WeekType.GetWeekTypes())
            {
                if (!dictionary.TryGetValue(key1, out var byKey1)) continue;
                if (!byKey1.TryGetValue(weekType, out var byWeekType)) continue;
                if (!byWeekType.TryGetValue(day, out var byDay)) continue;
                yield return byDay;
            }
        }

        public static bool HasMeetingsAtTime(this IEnumerable<Meeting?[]> days, int timeSlot)
        {
            return days.Select(d => d[timeSlot]).Any(m => m != null);
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

    //TODO pe плохое назчание у класса. Что за расширения?
    public static class Extensions
    {
        public const int MaxSpaces = 2 * 6 * 4; // weekTypes * daysOfWeek * maxSpaceCount

        public static CombinedEstimator GetDefaultCombinedEstimator()
        {
            var basic = (new FreedomDegreeEstimator(), 100);
            var groupsSpacesEstimator = (new StudentsSpacesEstimator(), 1);
            var teacherSpacesEstimator = (new TeacherSpacesEstimator(), 1);
            var meetingsPerDayEstimator = (new MeetingsPerDayEstimator(), 1);
            var teacherUsedDaysEstimator = (new TeacherUsedDaysEstimator(), 1);
            var teacherPriorityEstimator = (new TeacherPriorityEstimator(), 1);
            var estimator = new CombinedEstimator(basic, groupsSpacesEstimator,
                meetingsPerDayEstimator, teacherSpacesEstimator, teacherUsedDaysEstimator, teacherPriorityEstimator);
            return estimator;
        }

        public static ISolver GetSolver(SheetNamesConfig sheetNamesConfig, GsRepository repo)
        {
            var (requirements, learningPlan, _) = sheetNamesConfig;
            var (requisitions, _, classrooms) = SheetToRequisitionConverter.ConvertToRequisitions(
                repo, requirements, learningPlan, ClassroomsSheetName);

            var requisition = new Requisition(requisitions.ToArray());
            var estimator = GetDefaultCombinedEstimator();

            // return new GreedySolver(estimator, requisition, classrooms, new(42));
            return new RepeaterSolver(new GreedySolver(estimator, requisition, classrooms, new(228322), 3, true));
        }
    }
}