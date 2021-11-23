using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Enums;
using Domain.MeetingsParts;
using Infrastructure;
using static Domain.DomainExtensions;

namespace Domain.Algorithms.Estimators.GroupsEstimators
{
    public class DayDurationEstimator : GroupEstimator
    {
        public override double EstimateMeetingToAdd(Schedule schedule, Meeting meetingToAdd)
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

        private static string GetLogMessage(MeetingGroup group, WeekType weekType, DayOfWeek day, int count)
        {
            var weekTypeString = weekType.GetPrettyString();
            var dayString = day.GetPrettyString();
            return $"{group} has bad {weekTypeString} {dayString} with {count} day duration";
        }

        public override double GetMaxPenalty(Schedule schedule)
        {
            return schedule.GroupMeetingsByTime.Count * WeekTypesCount * MaxDaysCount;
        }

        private static int GetPenalty(IList<int> meetingsTimeSlots)
        {
            var last = meetingsTimeSlots.LastOrDefault();
            var count = last - meetingsTimeSlots.FirstOrDefault() + 1;
            if (count is >= 2 and <= 5 || last == default)
                return 0;
            return 1;
        }

        public override double GetScoreByGroup(MeetingGroup group, Schedule schedule, ILogger? logger = null)
        {
            var groupPenalty = 0;
            var scorePart = -1 / GetMaxPenalty(schedule);
            var byGroup = schedule.GroupMeetingsByTime[group];
            foreach (var (weekType, byWeekType) in byGroup)
            foreach (var (day, byDay) in byWeekType)
            {
                var meetingsTimeSlots = byDay.MeetingsTimeSlots();
                var last = meetingsTimeSlots.LastOrDefault();
                var count = last - meetingsTimeSlots.FirstOrDefault() + 1;
                if (count is >= 2 and <= 5 || last == default) continue;
                logger?.Log(GetLogMessage(group, weekType, day, count), scorePart);
                groupPenalty++;
            }

            return groupPenalty * scorePart;
        }
    }
}