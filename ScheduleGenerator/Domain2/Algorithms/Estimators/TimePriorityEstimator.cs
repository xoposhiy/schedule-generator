namespace Domain2.Algorithms.Estimators;

public class TimePriorityEstimator : IMeetingEstimator
{
    public double EstimateMeeting(State state, Meeting2 meeting)
    {
        var probabilityStorage = state.ProbabilityStorage;
        var priority = 0;
        for (; priority < meeting.MeetingTimePriorities.Count; priority++)
        {
            if (meeting.MeetingTimePriorities[priority].Contains(meeting.MeetingTime!))
                break;
        }

        return probabilityStorage.GetStudentsExpectation(meeting) *
               (-1d * priority / meeting.MeetingTimePriorities.Count);
    }
}