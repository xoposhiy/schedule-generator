namespace Domain2;

public class State
{
    public HashSet<Meeting2> NotPlacedMeetings;
    public List<Meeting2> PlacedMeetings = new List<Meeting2>();

    public State(IEnumerable<Meeting2> meetingsToPlace)
    {
        NotPlacedMeetings = meetingsToPlace.ToHashSet();
    }

    public void PlaceMeeting(Meeting2 meeting, MeetingTime meetingTime)
    {
        NotPlacedMeetings.Remove(meeting);
        PlacedMeetings.Add(meeting with {MeetingTime = meetingTime});
    }
}