namespace Domain2.Algorithms.Estimators;

public interface IStudentEstimator
{
    double EstimateStudentSchedule(List<Meeting2> meetings);
}