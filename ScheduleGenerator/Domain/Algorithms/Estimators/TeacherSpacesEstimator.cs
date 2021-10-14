using System;

namespace Domain.Algorithms.Estimators
{
    public class TeacherSpacesEstimator : IEstimator
    {
        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            throw new NotImplementedException();
        }

        public double Estimate(Schedule schedule)
        {
            var penalty = 0d;
            var teachers = schedule.TeacherMeetingsByTime.Values;
            foreach (var byTeacher in teachers)
            foreach (var byWeekType in byTeacher.Values)
            foreach (var byDay in byWeekType.Values)
            {
                penalty += byDay.GetMeetingsSpacesCount();
            }

            return -penalty / (teachers.Count * 2);
        }
    }
}