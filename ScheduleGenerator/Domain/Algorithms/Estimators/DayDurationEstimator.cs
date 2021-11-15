using System;
using System.Collections.Generic;
using System.Linq;
using Infrastructure;

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

                    meetingsTimeSlots = byDay.MeetingsTimeSlots();
                }

                var before = meetingsTimeSlots.LastOrDefault() - meetingsTimeSlots.FirstOrDefault();
                if (meetingToAdd.RequiredAdjacentMeeting != null)
                    meetingsTimeSlots.Add(meetingToAdd.RequiredAdjacentMeeting!.MeetingTime!.TimeSlot);
                meetingsTimeSlots.Add(timeSlot);
                meetingsTimeSlots.Sort();
                var after = meetingsTimeSlots.LastOrDefault() - meetingsTimeSlots.FirstOrDefault();

                var beforePenalty = GetPenalty(before);
                var afterPenalty = GetPenalty(after);

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
                var count = meetingsTimeSlots.LastOrDefault() - meetingsTimeSlots.FirstOrDefault();
                if (count is >= 2 and <= 4 or 0) continue;
                logger?.Log($"{group} has bad {weekType} {day} with {count} day duration", -1 / maxPenalty);
                penalty++;
            }

            return -penalty / maxPenalty;
        }

        private static double GetMaxPenalty(Schedule schedule)
        {
            return schedule.GroupMeetingsByTime.Count * 2 * 6;
        }

        private static int GetPenalty(int meetingsCount)
        {
            if (meetingsCount is >= 2 and <= 4 or 0) return 0;
            return 1;
        }
    }
}