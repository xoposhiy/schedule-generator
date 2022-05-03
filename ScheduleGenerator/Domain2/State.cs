using static CommonDomain.CommonDomainExtensions;

namespace Domain2;

public class State
{
    public Dictionary<int, Meeting2> NotPlacedMeetings;
    public List<Meeting2> PlacedMeetings = new();
    public ProbabilityStorage ProbabilityStorage = new();

    public State(IEnumerable<Meeting2> meetingsToPlace)
    {
        NotPlacedMeetings = meetingsToPlace.ToDictionary(m => m.Id, m => m);
    }

    private State(IEnumerable<Meeting2> meetingsToPlace, ProbabilityStorage probabilityStorage) : this(meetingsToPlace)
    {
        ProbabilityStorage = probabilityStorage;
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

    public void PlaceMeeting(Meeting2 meeting)
    {
        NotPlacedMeetings.Remove(meeting.Id);
        PlacedMeetings.Add(meeting);
    }

    public State Copy()
    {
        var copy = new State(NotPlacedMeetings.Values.Select(m => m with { }),
            ProbabilityStorage); //TODO: разобраться достаточно ли поверхностной копии митингов
        foreach (var meeting in PlacedMeetings.Select(m => m with { }))
        {
            copy.PlaceMeeting(meeting);
        }

        return copy;
    }
}