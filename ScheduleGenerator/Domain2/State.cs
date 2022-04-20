using static CommonDomain.CommonDomainExtensions;

namespace Domain2;

public class State
{
    public Dictionary<int, Meeting2> NotPlacedMeetings;
    public List<Meeting2> PlacedMeetings = new();

    public State(IEnumerable<Meeting2> meetingsToPlace)
    {
        NotPlacedMeetings = meetingsToPlace.ToDictionary(m => m.Id, m => m);
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
}