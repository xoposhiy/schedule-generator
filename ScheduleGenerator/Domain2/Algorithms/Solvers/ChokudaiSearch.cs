using CommonInfrastructure;
using Domain2.Algorithms.Estimators;

namespace Domain2.Algorithms.Solvers;

public class ChokudaiSearch : ISolver
{
    private readonly IMeetingEstimator estimator;

    public ChokudaiSearch(IMeetingEstimator estimator)
    {
        this.estimator = estimator;
    }

    public IEnumerable<(State schedule, double score)> GetSolutions(State problem, Countdown countdown)
    {
        var depth = problem.NotPlacedMeetings.Count + 1;
        var states = new List<PriorityQueue<State, double>>();
        for (var i = 0; i < depth + 1; i++) states.Add(new());

        states[0].Enqueue(problem, 0);

        while (!countdown.IsFinished())
            for (var t = 0; t < depth; t++)
            {
                if (!states[t].TryDequeue(out var state, out var penalty)) continue;

                var variants = state.GetAllPossibleVariants().ToList();

                if (variants.Count == 0) yield return (state, -penalty);

                foreach (var variant in variants)
                {
                    var scoreDelta = estimator.EstimateMeeting(state, variant);
                    var possible = state.AddMeeting(variant);
                    states[t + 1].Enqueue(possible, penalty - scoreDelta);
                }
            }
    }
}