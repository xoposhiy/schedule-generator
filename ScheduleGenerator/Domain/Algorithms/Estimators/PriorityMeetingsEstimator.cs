using System.Collections.Generic;
using System.Linq;

namespace Domain.Algorithms.Estimators
{
    public class PriorityMeetingsEstimator : IEstimator
    {
        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            throw new System.NotImplementedException();
        }

        public double Estimate(Schedule schedule, List<string>? logger = null)
        {
            var score = 0;
            foreach (var meeting in schedule.NotUsedMeetings.Where(m => m.Discipline.Name is "Физкультура" or "ИнЯз"))
            {
                logger?.Add($"{meeting} is not placed");
                score--;
            }

            return score;
        }
    }
}