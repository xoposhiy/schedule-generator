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
            var dayCount = 0;
            var teachersCount = schedule.TeacherMeetingsByTime.Count;

            foreach (var (teacher, byTeacher) in schedule.TeacherMeetingsByTime)
            foreach (var (weekType, byWeekType) in byTeacher)
            {
                var extraDays = Math.Max(0, byWeekType.Count - maxTeacherDays);
                if (extraDays > 0) logger?.Add($"{teacher} has {extraDays} extra days at {weekType} week");
                dayCount += extraDays;
            }

            var penalty = dayCount / 2;
            return -(penalty / teachersCount);
        }
    }
}