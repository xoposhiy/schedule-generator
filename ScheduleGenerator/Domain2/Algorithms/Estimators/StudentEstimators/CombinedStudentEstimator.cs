namespace Domain2.Algorithms.Estimators.StudentEstimators;

public class CombinedStudentEstimator : IStudentEstimator
{
    private List<IStudentEstimator> internalEstimators;

    public CombinedStudentEstimator(List<IStudentEstimator> internalEstimators)
    {
        this.internalEstimators = internalEstimators;
    }
    
    public double EstimateStudentSchedule(List<Meeting2> meetings)
    {
        return internalEstimators.Sum(e => e.EstimateStudentSchedule(meetings)) / internalEstimators.Count;
    }
}