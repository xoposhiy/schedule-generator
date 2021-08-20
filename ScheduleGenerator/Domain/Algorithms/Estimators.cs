using System;
using System.Linq;
using Domain.Conversions;
using Domain.ScheduleLib;

namespace Domain.Algorithms
{
    public class CombinedEstimator : IEstimator
    {
        private readonly IEstimator[] childEstimators;

        public CombinedEstimator(params IEstimator[] childEstimators)
        {
            this.childEstimators = childEstimators;
        }
        
        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            return childEstimators.Sum(e => e.Estimate(schedule, meetingToAdd));
        }

        public double Estimate(Schedule schedule)
        {
            return childEstimators.Sum(e => e.Estimate(schedule));
        }
    }

    public class BasicEstimator : IEstimator
    {
        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            throw new NotImplementedException();
        }

        public double Estimate(Schedule schedule)
        {
            var requisition = schedule.Requisition;
            var meetings = schedule.Meetings;
            var meetingsByTime = schedule.GroupMeetingsByTime;
            // Насколько много свободы у неоставленных пар. Проверять у всех непоставленных 
            // Добавить веса, вес этой вещи мниимальный
            return 0;
        }
    }
}