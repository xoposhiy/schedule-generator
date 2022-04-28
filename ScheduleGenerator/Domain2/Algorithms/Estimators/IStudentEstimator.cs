namespace Domain2.Algorithms.Estimators;

public interface IStudentEstimator
{
    double EstimateStudentSchedule(IEnumerable<Meeting2> meeting);
}