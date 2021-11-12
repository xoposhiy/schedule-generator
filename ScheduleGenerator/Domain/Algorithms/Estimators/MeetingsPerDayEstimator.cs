using Infrastructure;

namespace Domain.Algorithms.Estimators
{
    public class MeetingsPerDayEstimator : IEstimator
    {
        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            var groups = meetingToAdd.GroupsChoice!.GetGroupParts();
            var weekTypes = meetingToAdd.WeekType.GetWeekTypes();

            var penalty = 0;
            var maxPenalty = (double) groups.Count * weekTypes.Length;

            var dayOfWeek = meetingToAdd.MeetingTime!.Day;

            foreach (var meetingGroup in groups)
            foreach (var weekType in weekTypes)
            {
                var count = 1;
                if (schedule.GroupMeetingsByTime.TryGetValue(meetingGroup, weekType, dayOfWeek, out var day))
                    count += day.MeetingsCount();

                if (count is >= 2 and <= 4 or 0) continue;
                penalty++;
            }

            return -penalty / maxPenalty;
        }

        public double Estimate(Schedule schedule, ILogger? logger = null)
        {
            var penalty = 0d;

            double maxPenalty = schedule.GroupMeetingsByTime.Count * 2 * 6; // weekTypes * daysOfWeek

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
    }
}