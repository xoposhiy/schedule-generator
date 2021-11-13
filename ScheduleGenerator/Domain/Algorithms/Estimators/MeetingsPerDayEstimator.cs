using System;
using Infrastructure;

namespace Domain.Algorithms.Estimators
{
    public class MeetingsPerDayEstimator : IEstimator
    {
        
        
        public double EstimateMeetingToAdd(Schedule schedule, Meeting meetingToAdd)
        {
            var groups = meetingToAdd.GroupsChoice!.GetGroupParts();
            var weekTypes = meetingToAdd.WeekType.GetWeekTypes();

            var penaltyDelta = 0;

            var (dayOfWeek, timeSlot) = meetingToAdd.MeetingTime!;

            foreach (var meetingGroup in groups)
            foreach (var weekType in weekTypes)
            {
                var count = 0;
                if (schedule.GroupMeetingsByTime.TryGetValue(meetingGroup, weekType, dayOfWeek, out var day))
                {
                    if (day[timeSlot] != null)
                        throw new AggregateException("Placing meeting in taken place");
                    
                    count += day.MeetingsCount();
                }

                if (count == 1)
                    penaltyDelta--;
                if (count == 0 || count == 4)
                    penaltyDelta++;
            }

            return -penaltyDelta / GetMaxPenalty(schedule);
        }

        public double Estimate(Schedule schedule, ILogger? logger = null)
        {
            var penalty = 0d;

            foreach (var (group, byGroup) in schedule.GroupMeetingsByTime)
            foreach (var (weekType, byWeekType) in byGroup)
            foreach (var (day, byDay) in byWeekType)
            {
                var count = byDay.MeetingsCount();

                if (count is >= 2 and <= 4 or 0) continue;
                logger?.Log($"{group} has bad {weekType} {day} with {count} meetings", -1 / GetMaxPenalty(schedule));
                penalty++;
            }

            return -penalty / GetMaxPenalty(schedule);
        }

        private double GetMaxPenalty(Schedule schedule)
        {
            return schedule.GroupMeetingsByTime.Count * 2 * 6;
        }
    }
}