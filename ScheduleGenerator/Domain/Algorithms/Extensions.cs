using System;
using System.Collections.Generic;

namespace Domain.Algorithms
{
    public static class ArrayExtensions
    {
        public static IEnumerable<MeetingGroup> GetGroupParts(this MeetingGroup[] groups)
        {
            foreach (var group in groups)
                if (@group.GroupPart == GroupPart.FullGroup)
                {
                    yield return @group with {GroupPart = GroupPart.Part1};
                    yield return @group with {GroupPart = GroupPart.Part2};
                }
                else
                {
                    yield return @group;
                }
        }

        public static IEnumerable<WeekType> GetWeekTypes(this WeekType weekType)
        {
            if (weekType == WeekType.OddOrEven)
            {
                // throw new FormatException("Biba");
                yield return WeekType.Odd;
                yield break;
            }
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

        public static void SafeAdd<TKey1, TKey2, TValue>(this Dictionary<TKey1, Dictionary<TKey2, TValue>> dict,
            TKey1 key1, TKey2 key2, TValue value)
            where TKey1 : notnull
            where TKey2 : notnull
        {
            if (dict.ContainsKey(key1))
                dict[key1].Add(key2, value);
            else
                dict.Add(key1, new() {{key2, value}});
        }

        public static void SafeAdd<TKey1, TValue>(
            this Dictionary<TKey1, SortedSet<TValue>> dict, TKey1 key1, TValue value)
            where TKey1 : notnull
        {
            if (!dict.ContainsKey(key1)) dict[key1] = new();

            dict[key1].Add(value);
        }

        public static void SafeAdd<TKey1, TKey2, TValue>(
            this Dictionary<TKey1, Dictionary<TKey2, SortedSet<TValue>>> dict, TKey1 key1, TKey2 key2, TValue value)
            where TKey1 : notnull
            where TKey2 : notnull
        {
            if (!dict.ContainsKey(key1)) dict[key1] = new();

            if (!dict[key1].ContainsKey(key2)) dict[key1][key2] = new();

            dict[key1][key2].Add(value);
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