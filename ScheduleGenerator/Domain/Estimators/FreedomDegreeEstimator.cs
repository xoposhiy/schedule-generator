using System;
using System.Linq;
using Domain.Conversions;
using Domain.ScheduleLib;

namespace Domain.Estimators
{
    //TODO: fix estimator
    public class FreedomDegreeEstimator : IEstimator
    {
        public FreedomDegreeEstimator()
        {
            Weight = 100;
        }

        public double Weight { get; }

        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            throw new NotImplementedException();
        }

        public double Estimate(Schedule schedule)
        {
            // Насколько много свободы у неоставленных пар. Проверять у всех непоставленных 
            // Добавить веса, вес этой вещи мниимальный
            // var degreeOfFreedom = schedule.GetMeetingsToAdd().Count();
            var penalty = schedule.Meetings.Sum(meeting => schedule.MeetingFreedomDegree[meeting.BaseMeeting]);
            return -penalty;
        }
    }
}