using System;
using Infrastructure;

namespace Domain.Algorithms.Estimators
{
    public class TeacherSpacesEstimator : IEstimator
    {
        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            throw new NotImplementedException();
        }

        public double Estimate(Schedule schedule, ILogger? logger = null)
        {
            var penalty = 0d;

            double maxPenalty =
                schedule.TeacherMeetingsByTime.Count * 2 * 6 * 4; // weekTypes * daysOfWeek * maxSpaceCount

            // foreach (var (teacher, weekType, day, byDay) in schedule.TeacherMeetingsByTime.Enumerate())
            foreach (var (teacher, byGroup) in schedule.TeacherMeetingsByTime)
            foreach (var (weekType, byWeekType) in byGroup)
            foreach (var (day, byDay) in byWeekType)
            {
                var spacesCount = byDay.GetMeetingsSpacesCount();
                if (spacesCount == 0) continue;
                logger?.Log($"{teacher} has {spacesCount} spaces on {weekType} {day}", -spacesCount / maxPenalty);
                penalty += spacesCount;
            }

            return -penalty / maxPenalty;
        }
    }
}