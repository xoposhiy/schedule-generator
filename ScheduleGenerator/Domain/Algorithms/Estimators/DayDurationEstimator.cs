using System;
using System.Collections.Generic;
using System.Linq;
using Infrastructure;
using static Domain.DomainExtensions;

namespace Domain.Algorithms.Estimators
{
    public class DayDurationEstimator : IEstimator
    {
        public double EstimateMeetingToAdd(Schedule schedule, Meeting meetingToAdd)
        {
            var penaltyDelta = 0;
            var maxPenalty = GetMaxPenalty(schedule);

            var groups = meetingToAdd.GroupsChoice!.GetGroupParts();
            var weekTypes = meetingToAdd.WeekType.GetWeekTypes();

            var (dayOfWeek, timeSlot) = meetingToAdd.MeetingTime!;

            foreach (var meetingGroup in groups)
            foreach (var weekType in weekTypes)
            {
                var meetingsTimeSlots = new List<int>();
                if (schedule.GroupMeetingsByTime.TryGetValue(meetingGroup, weekType, dayOfWeek, out var byDay))
                {
                    if (byDay[timeSlot] != null)
                        throw new AggregateException("Placing meeting in taken place");

                    meetingsTimeSlots.AddRange(byDay.MeetingsTimeSlots());
                }

                var beforePenalty = GetPenalty(meetingsTimeSlots);

                meetingsTimeSlots.AddRange(meetingToAdd.GetLinkedMeetings().Select(m => m.MeetingTime!.TimeSlot));
                meetingsTimeSlots.Sort();

                var afterPenalty = GetPenalty(meetingsTimeSlots);

                penaltyDelta += afterPenalty - beforePenalty;
            }

            return -penaltyDelta / maxPenalty;
        }

        public double Estimate(Schedule schedule, ILogger? logger = null)
        {
            var penalty = 0;
            var maxPenalty = GetMaxPenalty(schedule);

            foreach (var (group, byGroup) in schedule.GroupMeetingsByTime)
            foreach (var (weekType, byWeekType) in byGroup)
            foreach (var (day, byDay) in byWeekType)
            {
                var meetingsTimeSlots = byDay.MeetingsTimeSlots();
                var last = meetingsTimeSlots.LastOrDefault();
                var count = last - meetingsTimeSlots.FirstOrDefault() + 1;
                if (count is >= 2 and <= 4 || last == default) continue;
                logger?.Log($"{group} has bad {weekType} {day} with {count} day duration", -1 / maxPenalty);
                penalty++;
            }

            return -penalty / maxPenalty;
        }

        private static double GetMaxPenalty(Schedule schedule)
        {
            return schedule.GroupMeetingsByTime.Count * WeekTypesCount * MaxDaysCount;
        }

        private static int GetPenalty(IList<int> meetingsTimeSlots)
        {
            var last = meetingsTimeSlots.LastOrDefault();
            var count = last - meetingsTimeSlots.FirstOrDefault() + 1;
            if (count is >= 2 and <= 4 || last == default)
                return 0;
            return 1;
        }
    }
}