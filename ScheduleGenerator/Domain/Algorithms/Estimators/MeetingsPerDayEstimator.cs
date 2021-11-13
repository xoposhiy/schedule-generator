using System;
using Infrastructure;

namespace Domain.Algorithms.Estimators
{
    public class MeetingsPerDayEstimator : IEstimator
    {
        public double EstimateMeetingToAdd(Schedule schedule, Meeting meetingToAdd)
        {
            var penaltyDelta = 0;
            var maxPenalty = GetMaxPenalty(schedule);

            var groups = meetingToAdd.GroupsChoice!.GetGroupParts();
            var weekTypes = meetingToAdd.WeekType.GetWeekTypes();

            var (dayOfWeek, timeSlot) = meetingToAdd.MeetingTime!;
            var meetingsCountDelta = meetingToAdd.RequiredAdjacentMeeting == null ? 1 : 2;

            foreach (var meetingGroup in groups)
            foreach (var weekType in weekTypes)
            {
                var before = 0;
                if (schedule.GroupMeetingsByTime.TryGetValue(meetingGroup, weekType, dayOfWeek, out var day))
                {
                    if (day[timeSlot] != null)
                        throw new AggregateException("Placing meeting in taken place");

                    before = day.MeetingsCount();
                }

                var after = before + meetingsCountDelta;

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
                var count = byDay.MeetingsCount();

                if (count is >= 2 and <= 4 or 0) continue;
                logger?.Log($"{group} has bad {weekType} {day} with {count} meetings", -1 / maxPenalty);
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