using static CommonDomain.Conversions;

namespace Domain2;

/// <summary>
/// Ковенция, которая позволяет считает новый хеш рассписания за O(1)
/// </summary>
public class ZobristHasher
{
    private readonly long[,] randomLongs;

    public ZobristHasher(IReadOnlyCollection<Meeting2> meetings)
    {
        var random = new Random();
        var tableSize = WeekDaysDict.Count * Constants.TimeSlots;
        randomLongs = new long[meetings.Count, tableSize];

        for (var i = 0; i < meetings.Count; i++)
        for (var j = 0; j < tableSize; j++)
            randomLongs[i, j] = random.NextInt64();
    }

    /// <summary>
    /// Хеш элемента расписания
    /// </summary>
    /// <param name="meeting2">Элемент расписания</param>
    /// <returns>Хеш</returns>
    public long GetMeetingHash(Meeting2 meeting2)
    {
        if (meeting2.Ignore) return 0;
        var time = meeting2.MeetingTime!;
        var timeCoordinate = time.TimeSlotIndex * WeekDaysDict.Count + (int) time.DayOfWeek;
        return randomLongs[meeting2.Id, timeCoordinate];
    }
}