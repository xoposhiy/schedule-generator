using System;
using System.Collections.Generic;

namespace Domain.Algorithms.Estimators
{
    public class MeetingsPerDayEstimator : IEstimator
    {
        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            throw new NotImplementedException();
        }

        public double Estimate(Schedule schedule, List<string>? logger = null)
        {
            var penalty = 0d;

            var maxPenalty = schedule.GroupMeetingsByTime.Count * 2 * 6; // weekTypes * daysOfWeek

            // foreach (var (group, weekType, day, byDay) in schedule.GroupMeetingsByTime.Enumerate())
            foreach (var (group, byGroup) in schedule.GroupMeetingsByTime)
            foreach (var (weekType, byWeekType) in byGroup)
            foreach (var (day, byDay) in byWeekType)
            {
                var count = byDay.MeetingsCount();

                if (count is not (>= 2 and <= 4 or 0))
                {
                    logger?.Add($"{group} has bad {weekType} {day} with {count} meetings");
                    penalty++;
                }
            }

            return -penalty / maxPenalty;
        }
    }
}