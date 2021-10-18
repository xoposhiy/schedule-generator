using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Algorithms.Estimators
{
    public class FreedomDegreeEstimator : IEstimator
    {
        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            throw new NotImplementedException();
        }

        public double Estimate(Schedule schedule, List<string>? logger = null)
        {
            var score = schedule.NotUsedMeetings.Sum(meeting => schedule.MeetingFreedomDegree[meeting]);
            return score;
        }
    }
}