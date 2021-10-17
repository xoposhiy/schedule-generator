using System;

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
            foreach (var day in byWeekType.Values)
            {
                var count = 0;
                for (var i = 0; i < 7; i++)
                {
                    if (day[i] != null) count++;
                }

                if (count is not (>= 2 and <= 4))
                {
                    penalty++;
                }
            }

            return -penalty / (groups.Count * 2);
            // TODO поделить на количество половинок групп и количество дней и 2 (количество четностей недель)
        }
    }
}