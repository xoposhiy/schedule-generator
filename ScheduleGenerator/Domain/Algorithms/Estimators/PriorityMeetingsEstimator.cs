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
            foreach (var meeting in schedule.NotUsedMeetings.Where(m => m.Discipline.Name is "Физкультура" or "ИнЯз"))
            {
                logger?.Log($"{meeting} is not placed", -1);
                score--;
            }

            return score;
        }
    }
}