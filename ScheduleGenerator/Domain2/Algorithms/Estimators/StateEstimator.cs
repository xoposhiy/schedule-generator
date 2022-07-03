namespace Domain2.Algorithms.Estimators;

public class StateEstimator : IStateEstimator
{
    private readonly IMeetingEstimator meetingEstimator;

    public StateEstimator(IMeetingEstimator meetingEstimator)
    {
        this.meetingEstimator = meetingEstimator;
    }

    public double EstimateSchedule(State state)
    {
        var score = 0d;
        var meetings = state.PlacedMeetings;
        var problem = new State(meetings, state.ProbabilityStorage);
        foreach (var meeting in meetings)
        {
            score += meetingEstimator.EstimateMeeting(problem, meeting);
            problem = problem.AddMeeting(meeting);
        }

        return score;
    }
}