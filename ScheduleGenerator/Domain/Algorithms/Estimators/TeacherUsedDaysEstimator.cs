using System;

namespace Domain.Algorithms.Estimators
{
    public class TeacherUsedDaysEstimator : IEstimator
    {
        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            throw new NotImplementedException();
        }

        public double Estimate(Schedule schedule)
        {
            const int maxTeacherDays = 1;
            var dayCount = 0;

            foreach (var byTeacher in schedule.TeacherMeetingsByTime.Values)
            foreach (var byWeekType in byTeacher.Values)
                dayCount += byWeekType.Keys.Count;

            var penalty = dayCount / 2 - maxTeacherDays;
            return penalty < 0 ? 0 : -penalty;
        }
    }
}