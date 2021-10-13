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
            
            var teachers = schedule.TeacherMeetingsByTime.Values;
            
            foreach (var byTeacher in teachers)
            foreach (var byWeekType in byTeacher.Values)
                dayCount += byWeekType.Keys.Count;

            var penalty = dayCount / 2;
            return -(penalty / teachers.Count);
        }
    }
}