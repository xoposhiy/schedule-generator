using System;
using System.Linq;
using Domain.Conversions;
using Domain.ScheduleLib;

namespace Domain.Estimators
{
    public class TeacherSpacesEstimator : IEstimator
    {
        public TeacherSpacesEstimator()
        {
            Weight = 1;
        }

        public double Weight { get; }

        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            throw new NotImplementedException();
        }

        public double Estimate(Schedule schedule)
        {
            var score = 0;
            foreach (var day in schedule.TeacherMeetingsTimesByDay.Keys)
            {
                foreach (var teacher in schedule.TeacherMeetingsTimesByDay[day].Keys)
                {
                    var previousTimeslot = schedule.TeacherMeetingsTimesByDay[day][teacher].FirstOrDefault();
                    foreach (var timeslot in schedule.TeacherMeetingsTimesByDay[day][teacher].Skip(1))
                    {
                        score += timeslot - previousTimeslot - 1;
                        // previousTimeslot = timeslot;
                    }
                }
            }
            return -score;
        }
    }
}