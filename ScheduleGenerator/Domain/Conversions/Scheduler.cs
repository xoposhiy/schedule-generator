using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.ScheduleLib;

namespace Domain.Conversions
{
    public record Solution(Schedule Schedule, double Score);

    public interface IEstimator
    {
        double Weight { get; }
        double Estimate(Schedule schedule, Meeting meetingToAdd);
        double Estimate(Schedule schedule);
    }

    public interface ISolver
    {
        /// <returns>Последовательность все более хороших решений</returns>
        IEnumerable<Solution> GetSolution(TimeSpan timeBudget);
    }
}