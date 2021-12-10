using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using static Infrastructure.LoggerExtension;
using static Domain.DomainExtensions;

namespace Domain.Algorithms.Solvers
{
    public class RepeaterSolver : ISolver
    {
        private readonly ISolver solver;

        public RepeaterSolver(ISolver solver)
        {
            this.solver = solver;
        }

        public Solution GetSolution(TimeSpan timeBudget)
        {
            var justiceEstimator = GetDefaultJusticeEstimator();
            var sw = Stopwatch.StartNew();

            Solution bestSolution = solver.GetSolution(timeBudget - sw.Elapsed);
            // 1. score + 2 * justice
            // // 1. score + 0.5 * justice
            // 2. (score, justice)
            // var bestJustice = justiceEstimator.Estimate(bestSolution.Schedule);

            var scoreSum = bestSolution.Score;
            var iteration = 1;
            var improvementsCount = 0;
            var bestIteration = 0;

            var solutions = Enumerable.Repeat(0, int.MaxValue)
                .AsParallel()
                .Select(_ => solver.GetSolution(timeBudget - sw.Elapsed))
                .Select(sol =>
                {
                    var (schedule, score) = sol;
                    var justice = justiceEstimator.Estimate(schedule);
                    return new Solution(schedule, score + 0.5 * justice);
                });
            foreach (var solution in solutions)
            {
                Interlocked.Increment(ref iteration);
                // iteration++;
                // var justice = justiceEstimator.Estimate(schedule);
                // var solution = new Solution(schedule, score + 0.5 * justice);
                // var solution = new Solution(schedule, score);
                scoreSum += solution.Score;
                if (IsSolutionBetter(solution, bestSolution))
                {
                    // WriteLog($"justice: {justice}");
                    bestSolution = solution;
                    improvementsCount++;
                    bestIteration = iteration;
                    var message = GetImprovementMessage(improvementsCount, iteration, solution);
                    WriteLog(message);
                    // WriteLog($"Score + Justice = {solution.Score + justice}");
                }

                if (sw.Elapsed > timeBudget) break;
            }

            sw.Stop();

            var finalMessage = GetFinalMessage(sw, iteration, bestSolution, bestIteration, improvementsCount, scoreSum);
            WriteLog(finalMessage);
            return bestSolution;
        }

        public Solution Solve(Schedule schedule, TimeSpan timeBudget)
        {
            throw new NotImplementedException();
        }

        private static bool IsSolutionBetter(Solution solution, Solution bestSolution)
        {
            var (schedule, score) = solution;
            var (bestSchedule, bestScore) = bestSolution;
            if (schedule.NotUsedMeetings.Count > bestSchedule.NotUsedMeetings.Count) return false;
            if (schedule.NotUsedMeetings.Count < bestSchedule.NotUsedMeetings.Count)
            {
                // WriteLog(
                //     $"Improvement reason: Not placed meetings count {bestSchedule.NotUsedMeetings.Count} -> {schedule.NotUsedMeetings.Count}");
                return true;
            }

            if (score < bestScore) return false;
            if (score > bestScore)
            {
                // WriteLog($"Improvement reason: Score better {bestScore} -> {score}");
                return true;
            }

            // if (justice > bestJustice)
            // {
            //     WriteLog($"Improvement reason: Justice better {bestJustice} -> {justice}");
            //     return true;
            // }

            return false;
        }

        private static string GetImprovementMessage(int improvements, int iteration, Solution solution)
        {
            var (schedule, score) = solution;
            return $"{improvements} improvement in {iteration} iteration. " +
                   $"Not Placed: {schedule.NotUsedMeetings.Count} score: {score}";
        }

        private static string GetFinalMessage(Stopwatch sw, int repeats, Solution bestSolution, int bestIteration,
            int improvementsCount, double scoreSum)
        {
            var (schedule, score) = bestSolution;
            var lines = new[]
            {
                "",
                $"Repeater: {sw.Elapsed}",
                $"Total repeats: {repeats}",
                $"Improvements count: {improvementsCount}",
                $"Best solution iteration: {bestIteration}",
                $"Mean elapsed: {sw.Elapsed / repeats}",
                $"Mean Score: {scoreSum / repeats}",
                "",
                $"Not placed meetings: {schedule.NotUsedMeetings.Count}",
                $"Placed meetings: {schedule.Meetings.Count}",
                $"Score: {score}",
                ""
            };
            return string.Join("\n", lines);
        }
    }
}