namespace Domain2.Algorithms.Estimators;

public class LateMeetingsEstimator : IMeetingEstimator
{
    private const int LatestOfflineMeeting = 6;

    public double EstimateMeeting(State state, Meeting2 meeting)
    {
        if (meeting.Ignore || meeting.Place == "Онлайн") 
            return 0;
        var timeSlot = meeting.MeetingTime!.TimeSlot;
        if (timeSlot < LatestOfflineMeeting) return 0;
        return LatestOfflineMeeting - timeSlot;
    }
}