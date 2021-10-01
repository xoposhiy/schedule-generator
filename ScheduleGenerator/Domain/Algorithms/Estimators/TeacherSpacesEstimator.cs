using System;
using System.Linq;

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
            var penalty = 0;
            foreach (var day in schedule.TeacherMeetingsTimesByDay.Keys)
            foreach (var teacher in schedule.TeacherMeetingsTimesByDay[day].Keys)
            {
                //TODO четные и нечетные недели оценивать отдельно и складывать их результаты
                var previousTimeslot = schedule.TeacherMeetingsTimesByDay[day][teacher].FirstOrDefault();
                foreach (var timeslot in schedule.TeacherMeetingsTimesByDay[day][teacher].Skip(1))
                    penalty += timeslot - previousTimeslot - 1;
                // previousTimeslot = timeslot;
            }

            return -penalty;
        }
    }
}