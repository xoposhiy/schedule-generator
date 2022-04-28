namespace Domain2.Algorithms.Estimators;

public class CombinedMeetingEstimator : IMeetingEstimator
{
    private readonly IMeetingEstimator[] estimators;

    public CombinedMeetingEstimator(IMeetingEstimator[] estimators)
    {
        this.estimators = estimators.ToArray();
    }
    
    public double EstimateMeeting(State state, Meeting2 meeting)
    {
        return estimators.Sum(e => e.EstimateMeeting(state, meeting));
    }
}