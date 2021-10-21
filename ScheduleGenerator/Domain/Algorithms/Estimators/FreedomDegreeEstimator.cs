using System;
using System.Linq;

namespace Domain.Algorithms.Estimators
{
    public class FreedomDegreeEstimator : IEstimator
    {
        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            throw new NotImplementedException();
        }

        public double Estimate(Schedule schedule, ILogger? logger = null)
        {
            var score = schedule.NotUsedMeetings.Sum(meeting => schedule.MeetingFreedomDegree[meeting]);
            return score;
        }
    }
}