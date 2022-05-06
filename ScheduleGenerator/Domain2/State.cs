using System.Collections.Immutable;
using static CommonDomain.CommonDomainExtensions;

namespace Domain2;

public class State
{
    private readonly long hashCode;
    public readonly ImmutableDictionary<int, Meeting2> NotPlacedMeetings;
    public readonly ImmutableList<Meeting2> PlacedMeetings;
    public readonly ProbabilityStorage ProbabilityStorage;
    public readonly ZobristHasher ZobristHasher;

    public State(IReadOnlyCollection<Meeting2> meetingsToPlace, ProbabilityStorage probabilityStorage)
    {
        PlacedMeetings = ImmutableList<Meeting2>.Empty;
        NotPlacedMeetings = meetingsToPlace.ToImmutableDictionary(m => m.Id, m => m);
        ProbabilityStorage = probabilityStorage;
        ZobristHasher = new(meetingsToPlace);
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
        ZobristHasher = hasher;
        this.hashCode = hashCode;
    }

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

    public State AddMeeting(Meeting2 meeting)
    {
        var placedMeetings = PlacedMeetings.Add(meeting);
        var dictionary = NotPlacedMeetings.Remove(meeting.Id);
        var hash = hashCode ^ ZobristHasher.GetMeetingHash(meeting);
        return new(placedMeetings, dictionary, ProbabilityStorage, ZobristHasher, hash);
    }
}