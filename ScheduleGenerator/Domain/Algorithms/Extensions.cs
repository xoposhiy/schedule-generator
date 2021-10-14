using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Domain.Algorithms
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

        public static int GetMeetingsSpacesCount(this Dictionary<DayOfWeek, Meeting?[]> dictionary)
        {
            var count = 0;
            
            foreach (var byDay in dictionary.Values)
            {
                var i = 0;
                var prev = 0;
                for (; i < 7; i++)
                {
                    if (byDay[i] != null)
                    {
                        prev = i;
                        break;
                    }
                }

                for (; i < 7; i++)
                {
                    if (byDay[i] != null)
                    {
                        count += i - prev + 1;
                        prev = i;
                    }
                }
                // var orderedSlots = byDay.ToImmutableSortedSet();
                // for (var i = 1; i < orderedSlots.Count; i++) count += orderedSlots[i] - orderedSlots[i - 1] - 1;
            }

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

        public static void SafeIncrement<TKey1, TKey2>(
            this Dictionary<TKey1, Dictionary<TKey2, int>> dict,
            TKey1 key1, TKey2 key2)
            where TKey1 : notnull
            where TKey2 : notnull
        {
            if (!dict.ContainsKey(key1)) dict[key1] = new();

            if (!dict[key1].ContainsKey(key2)) dict[key1][key2] = 0;

            dict[key1][key2]++;
        }

        public static void SafeDecrement<TKey1, TKey2>(
            this Dictionary<TKey1, Dictionary<TKey2, int>> dict,
            TKey1 key1, TKey2 key2)
            where TKey1 : notnull
            where TKey2 : notnull
        {
            if (!dict.ContainsKey(key1)) throw new FormatException($"Dictionary does not contains key1: {key1}");

            if (!dict[key1].ContainsKey(key2)) throw new FormatException($"Dictionary does not contains key2: {key2}");

            if (dict[key1][key2] == 0)
                return;
            dict[key1][key2]--;
        }
    }
}