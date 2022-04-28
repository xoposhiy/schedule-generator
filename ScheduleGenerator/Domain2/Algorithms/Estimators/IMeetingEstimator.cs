namespace Domain2.Algorithms.Estimators;

public interface IMeetingEstimator
{
    double EstimateMeeting(State state, Meeting2 meeting);
}