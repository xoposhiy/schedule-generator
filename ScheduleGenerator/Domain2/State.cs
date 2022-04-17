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
}