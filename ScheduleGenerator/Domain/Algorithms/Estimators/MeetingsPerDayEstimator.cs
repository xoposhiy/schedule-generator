using System;
using System.Linq;

namespace Domain.Algorithms.Estimators
{
    public class MeetingsPerDayEstimator : IEstimator
    {
        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            throw new NotImplementedException();
        }

        public double Estimate(Schedule schedule)
        {
            var penalty = 0d;
            var groups = schedule.GroupMeetingsByTime.Values;
            foreach (var byGroup in groups)
            foreach (var byWeekType in byGroup.Values)
                penalty += byWeekType.Values
                    .Select(g => g.Count(m => m != null))
                    .Count(c => c is not (>= 2 and <= 4));

            return -penalty / (groups.Count * 2);
            // TODO поделить на количество половинок групп и количество дней и 2 (количество четностей недель)
        }
    }
}