namespace Domain2;

public class State
{
    public List<Meeting2> NotPlacedMeetings = new List<Meeting2>();
    public List<Meeting2> PlacedMeetings = new List<Meeting2>();

    public State(List<Meeting2> meetingsToPlace)
    {
        NotPlacedMeetings = meetingsToPlace;
    }
}