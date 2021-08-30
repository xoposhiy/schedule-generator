using System;
using Domain.Conversions;
using Domain.ScheduleLib;

namespace Domain.Estimators
{
    public class MeetingsPerDayEstimator : IEstimator
    {
        public MeetingsPerDayEstimator()
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
            const int maxMeetingsPerDay = 3;
            foreach (var day in schedule.GroupsMeetingsTimesByDay.Keys)
            {
                foreach (var groupName in schedule.GroupsMeetingsTimesByDay[day].Keys)
                {
                    foreach (var groupPart in schedule.GroupsMeetingsTimesByDay[day][groupName].Keys)
                    {
                        score += schedule.GroupsMeetingsTimesByDay[day][groupName][groupPart].Count - maxMeetingsPerDay;
                    }
                }
            }
            return -score;
        }
    }
}