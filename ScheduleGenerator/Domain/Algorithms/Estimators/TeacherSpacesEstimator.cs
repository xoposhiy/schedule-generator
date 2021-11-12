using Infrastructure;
using static Domain.DomainExtensions;

namespace Domain.Algorithms.Estimators
{
    public class TeacherSpacesEstimator : IEstimator
    {
        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            var teacher = meetingToAdd.Teacher;

            var penalty = GetSpacesPenalty(meetingToAdd, teacher, schedule.TeacherMeetingsByTime);

            return -penalty;
        }

        public double Estimate(Schedule schedule, ILogger? logger = null)
        {
            var penalty = 0d;

            double maxPenalty = schedule.TeacherMeetingsByTime.Count * MaxSpaces;

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