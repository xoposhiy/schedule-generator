using System;
using System.Linq;
using Domain.Conversions;
using Domain.ScheduleLib;

namespace Domain.Estimators
{
    public class FreedomDegreeEstimator : IEstimator
    {
        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            throw new NotImplementedException();
        }

        public double Estimate(Schedule schedule)
        {
            // Насколько много свободы у неоставленных пар. Проверять у всех непоставленных 
            // Добавить веса, вес этой вещи мниимальный
            // var degreeOfFreedom = schedule.GetMeetingsToAdd().Count();
            var penalty = schedule.Meetings.Sum(meeting => schedule.MeetingFreedomDegree[meeting.BaseMeeting!]);
            return -penalty;
        }
    }
}