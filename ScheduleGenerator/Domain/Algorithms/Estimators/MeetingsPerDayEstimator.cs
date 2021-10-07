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
            var penalty = 0;
            foreach (var byGroup in schedule.GroupMeetingsByTime.Values)
            foreach (var byWeekType in byGroup.Values)
                penalty += byWeekType.Keys.GroupBy(t => t.Day)
                    .Select(g => g.Count())
                    .Count(c => c is not (>= 2 and <= 4));

            return -penalty;
            // TODO поделить на количество половинок групп и количество дней и 2 (количество четностей недель)
        }
    }
}