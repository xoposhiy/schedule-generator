namespace Domain2.Algorithms.Estimators;

public interface IStateEstimator
{
    double EstimateSchedule(State state);
}