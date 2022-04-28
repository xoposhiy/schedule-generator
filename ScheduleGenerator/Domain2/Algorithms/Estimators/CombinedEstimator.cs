namespace Domain2.Algorithms.Estimators;

public class CombinedEstimator : IEstimator
{
    public readonly IEstimator[] estimators;

    public CombinedEstimator(IEstimator[] estimators)
    {
        this.estimators = estimators.ToArray();
    }
    
    public double EstimateMeeting(State state, Meeting2 meeting)
    {
        return estimators.Sum(e => e.EstimateMeeting(state, meeting));
    }
}