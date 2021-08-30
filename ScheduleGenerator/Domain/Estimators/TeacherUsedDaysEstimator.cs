using System;
using Domain.Conversions;
using Domain.ScheduleLib;

namespace Domain.Estimators
{
    public class TeacherUsedDaysEstimator : IEstimator
    {
        public TeacherUsedDaysEstimator()
        {
            Weight = 10;
        }

        public double Weight { get; }

        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            throw new NotImplementedException();
        }

        public double Estimate(Schedule schedule)
        {
            const int maxTeacherDays = 1;
            var dayCount = 0;
            foreach (var day in schedule.TeacherMeetingsTimesByDay.Keys)
            {
                foreach (var teacher in schedule.TeacherMeetingsTimesByDay[day].Keys)
                {
                    dayCount += schedule.TeacherMeetingsTimesByDay[day][teacher].Count == 0 ? 0 : 1;
                }
            }
            var score = dayCount - maxTeacherDays;
            return score < 0 ? 0 : -score;
        }
    }
}