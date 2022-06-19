using System.Collections.Immutable;
using static CommonDomain.CommonDomainExtensions;

namespace Domain2;

/// <summary>
/// Состояние расписания
/// </summary>
public class State
{
    /// <summary>
    /// Хеш, полученный ZobristHasher-ом
    /// </summary>
    public readonly long HashCode;

    /// <summary>
    /// Пары, которые еще не поставленны в расписание
    /// </summary>
    public readonly ImmutableDictionary<int, Meeting2> NotPlacedMeetings;

    /// <summary>
    /// Поставленные пары
    /// </summary>
    public readonly ImmutableList<Meeting2> PlacedMeetings;

    /// <summary>
    /// Место с вероятностями попадания студентов на курсы
    /// </summary>
    public readonly ProbabilityStorage ProbabilityStorage;

    /// <summary>
    /// Алгоримт хеширования, который помогает поддерживать HashCode
    /// </summary>
    private readonly ZobristHasher zobristHasher;

    public State(IReadOnlyCollection<Meeting2> meetingsToPlace, ProbabilityStorage probabilityStorage)
    {
        PlacedMeetings = ImmutableList<Meeting2>.Empty;
        NotPlacedMeetings = meetingsToPlace.ToImmutableDictionary(m => m.Id, m => m);
        ProbabilityStorage = probabilityStorage;
        zobristHasher = new(meetingsToPlace);
    }

    private State(ImmutableList<Meeting2> placedMeetings,
        ImmutableDictionary<int, Meeting2> notPlacedMeetings,
        ProbabilityStorage probabilityStorage,
        ZobristHasher hasher,
        long hashCode)
    {
        PlacedMeetings = placedMeetings;
        NotPlacedMeetings = notPlacedMeetings;
        ProbabilityStorage = probabilityStorage;
        zobristHasher = hasher;
        HashCode = hashCode;
    }

    /// <summary>
    /// Возвращает все пары, которые идут в указанное время
    /// </summary>
    /// <param name="meetingTime">Время проведения пары</param>
    public IEnumerable<Meeting2> this[MeetingTime meetingTime]
    {
        get
        {
            foreach (var meeting in PlacedMeetings)
            {
                if (meeting.Ignore) continue;
                var (weekType, dayOfWeek, timeSlot) = meeting.MeetingTime!;
                if (dayOfWeek != meetingTime.DayOfWeek) continue;
                if (!WeekTypesIntersects(weekType, meetingTime.WeekType)) continue;

                if (timeSlot <= meetingTime.TimeSlot && meetingTime.TimeSlot < timeSlot + meeting.Duration)
                    yield return meeting;
            }
        }
    }

    /// <summary>
    /// Добавление пары в рассписание
    /// </summary>
    /// <param name="meeting">Пара</param>
    /// <returns>Состояние расписания, после постановки пары</returns>
    public State AddMeeting(Meeting2 meeting)
    {
        var placedMeetings = PlacedMeetings.Add(meeting);
        var dictionary = NotPlacedMeetings.Remove(meeting.Id);
        var hash = HashCode ^ zobristHasher.GetMeetingHash(meeting);
        return new(placedMeetings, dictionary, ProbabilityStorage, zobristHasher, hash);
    }
}