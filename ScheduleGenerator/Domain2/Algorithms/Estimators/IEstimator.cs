namespace Domain2.Algorithms;

public interface IEstimator
{
    double EstimateMeeting(State state, Meeting2 meeting);
}