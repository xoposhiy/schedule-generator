namespace Domain2.Algorithms.Estimators;

public class LateMeetingsEstimator : IMeetingEstimator
{
    private const int LatestOfflineMeeting = 6;
    private const int LatestOnlineMeeting = 7;

    public double EstimateMeeting(State state, Meeting2 meeting)
    {
        var timeSlot = meeting.MeetingTime!.TimeSlot;
        if (meeting.Ignore) 
            return 0;
        if (meeting.Place == "Онлайн")
        {
            if (timeSlot < LatestOnlineMeeting) return 0;
            return LatestOnlineMeeting - timeSlot;
        }
        else
        {
            if (timeSlot < LatestOfflineMeeting) return 0;
            return LatestOfflineMeeting - timeSlot;
        }
    }
}