using System;
using System.Collections.Generic;

namespace Domain.Algorithms.Estimators
{
    public class TeacherUsedDaysEstimator : IEstimator
    {
        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            throw new NotImplementedException();
        }

        public double Estimate(Schedule schedule, List<string>? logger = null)
        {
            const int maxTeacherDays = 2;
            var penalty = 0d;
            var teachersCount = schedule.TeacherMeetingsByTime.Count * 2 * 4; // weekTypes * maxExtraDays

            foreach (var (teacher, byTeacher) in schedule.TeacherMeetingsByTime)
            foreach (var (weekType, byWeekType) in byTeacher)
            {
                var days = 0;
                foreach (var day in byWeekType.Values)
                    if (day.MeetingsCount() != 0)
                        days++;

                var extraDays = Math.Max(0, days - maxTeacherDays);
                if (extraDays > 0) logger?.Add($"{teacher} has {extraDays} extra days at {weekType} week");
                penalty += extraDays;
            }

            return -(penalty / teachersCount);
        }
    }
}