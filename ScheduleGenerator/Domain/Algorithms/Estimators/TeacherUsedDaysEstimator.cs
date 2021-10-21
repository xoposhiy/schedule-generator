using System;

namespace Domain.Algorithms.Estimators
{
    public class TeacherUsedDaysEstimator : IEstimator
    {
        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            throw new NotImplementedException();
        }

        public double Estimate(Schedule schedule, ILogger? logger = null)
        {
            const int maxTeacherDays = 2;
            var penalty = 0d;
            double maxPenalty = schedule.TeacherMeetingsByTime.Count * 2 * 4; // weekTypes * maxExtraDays

            foreach (var (teacher, byTeacher) in schedule.TeacherMeetingsByTime)
            foreach (var (weekType, byWeekType) in byTeacher)
            {
                var days = 0;
                foreach (var day in byWeekType.Values)
                    if (day.MeetingsCount() != 0)
                        days++;

                var extraDays = Math.Max(0, days - maxTeacherDays);
                if (extraDays == 0) continue;
                logger?.Log($"{teacher} has {extraDays} extra days at {weekType} week", -extraDays / maxPenalty);
                penalty += extraDays;
            }

            return -penalty / maxPenalty;
        }
    }
}