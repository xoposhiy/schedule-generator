namespace Domain2.Algorithms.Estimators;

public interface IEstimator
{
    double EstimateMeeting(State state, Meeting2 meeting);
}