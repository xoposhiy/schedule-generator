using Infrastructure;
using static Domain.DomainExtensions;

namespace Domain.Algorithms.Estimators
{
    public class TeacherSpacesEstimator : IEstimator
    {
        public double EstimateMeetingToAdd(Schedule schedule, Meeting meetingToAdd)
        {
            var teacher = meetingToAdd.Teacher;

            var maxPenalty = GetMaxPenalty(schedule);

            var penaltyDelta = GetSpacesCountDelta(meetingToAdd, teacher, schedule.TeacherMeetingsByTime);

            return -penaltyDelta / maxPenalty;
        }

        public double Estimate(Schedule schedule, ILogger? logger = null)
        {
            var penalty = 0d;
            var maxPenalty = GetMaxPenalty(schedule);

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

        private static double GetMaxPenalty(Schedule schedule)
        {
            return schedule.TeacherMeetingsByTime.Count * MaxSpaces;
        }
    }
}