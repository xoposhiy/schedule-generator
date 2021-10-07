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
            foreach (var byDay in schedule.TeacherMeetingsTimesByDay.Values)
            foreach (var byTeacher in byDay.Values)
                dayCount += Math.Max(byTeacher.Count, 1);
            var penalty = dayCount - maxTeacherDays;
            return penalty < 0 ? 0 : -penalty;
        }
    }
}