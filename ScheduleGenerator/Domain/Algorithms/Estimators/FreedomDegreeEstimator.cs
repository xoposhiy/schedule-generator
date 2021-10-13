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

        public double Estimate(Schedule schedule)
        {
            var penalty = schedule.Meetings.Sum(meeting => schedule.MeetingFreedomDegree[meeting.BaseMeeting!]);
            return -penalty;
        }
    }
}