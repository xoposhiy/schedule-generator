using CommonInfrastructure;
using Domain2.Algorithms.Estimators;

namespace Domain2.Algorithms.Solvers;

public class ChokudaiSearch : ISolver
{
    private readonly IMeetingEstimator estimator;
    private readonly int maxBeamWidth;

    public ChokudaiSearch(int maxBeamWidth, IMeetingEstimator estimator)
    {
        this.maxBeamWidth = maxBeamWidth;
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
                if (!states[t].TryDequeue(out var state, out var score)) continue;

                var variants = state.GetAllPossibleVariants().ToList();

                if (variants.Count == 0) yield return (state, -score);

                foreach (var variant in variants)
                {
                    var possible = state.Copy();
                    var scoreDelta = estimator.EstimateMeeting(possible, variant);
                    possible.PlaceMeeting(variant);
                    states[t + 1].Enqueue(possible, score - scoreDelta);
                }
            }
    }
}