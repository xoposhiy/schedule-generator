using System;
using System.Linq;
using Domain.Conversions;
using Domain.ScheduleLib;

namespace Domain.Estimators
{
    public class StudentsSpacesEstimator : IEstimator
    {
        public StudentsSpacesEstimator()
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
            //TODO придумать как учитывать пары, которые идут не весь семестр.
            var score = 0;
            foreach (var day in schedule.GroupsMeetingsTimesByDay.Keys)
            {
                foreach (var groupName in schedule.GroupsMeetingsTimesByDay[day].Keys)
                {
                    foreach (var groupPart in schedule.GroupsMeetingsTimesByDay[day][groupName].Keys)
                    {
                        //TODO четные и нечетные недели оценивать отдельно и складывать их результаты
                        var previousTimeslot = schedule.GroupsMeetingsTimesByDay[day][groupName][groupPart].FirstOrDefault();
                        foreach (var timeslot in schedule.GroupsMeetingsTimesByDay[day][groupName][groupPart].Skip(1))
                        {
                            score += timeslot - previousTimeslot - 1;
                            // previousTimeslot = timeslot;
                        }
                    }
                }
            }
            return -score;
        }
    }
}