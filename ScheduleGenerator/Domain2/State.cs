using CommonDomain.Enums;

namespace Domain2;

public class State
{
    public Dictionary<int, Meeting2> NotPlacedMeetings;
    public List<Meeting2> PlacedMeetings = new();

    public State(IEnumerable<Meeting2> meetingsToPlace)
    {
        NotPlacedMeetings = meetingsToPlace.ToDictionary(m => m.Id, m => m);
    }

    public void PlaceMeeting(Meeting2 meeting)
    {
        NotPlacedMeetings.Remove(meeting.Id);
        PlacedMeetings.Add(meeting);
    }

    public IEnumerable<Meeting2> this[MeetingTime meetingTime]
    {
        get
        {
            foreach (var meeting in PlacedMeetings)
            {
                var (weekType, dayOfWeek, timeSlot) = meeting.MeetingTime!;
                if (dayOfWeek != meetingTime.DayOfWeek) continue;
                if (weekType != WeekType.All && weekType != meetingTime.WeekType) continue;
                
                if (timeSlot <= meetingTime.TimeSlot && meetingTime.TimeSlot < timeSlot + meeting.Duration)
                    yield return meeting;
            }
        }
    }
}