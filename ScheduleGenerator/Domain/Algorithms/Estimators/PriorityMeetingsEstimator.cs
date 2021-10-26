using System.Linq;
using Infrastructure;

namespace Domain.Algorithms.Estimators
{
    public class PriorityMeetingsEstimator : IEstimator
    {
        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            throw new System.NotImplementedException();
        }

        public double Estimate(Schedule schedule, ILogger? logger = null)
        {
            var score = 0;
            foreach (var meeting in schedule.NotUsedMeetings.Where(m => m.Priority != 0))
            {
                logger?.Log($"{meeting} is not placed", -1 * (1 << (2 * meeting.Priority)));
                score -= 1 << (2 * meeting.Priority);
            }

            return score;
        }
    }
}