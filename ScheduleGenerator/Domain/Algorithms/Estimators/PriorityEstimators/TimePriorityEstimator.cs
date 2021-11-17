using System;

namespace Domain.Algorithms.Estimators.PriorityEstimators
{
    public class TimePriorityEstimator : PriorityEstimator
    {
        protected override string GetLogMessage(Meeting meeting, double priorityPenalty)
        {
            var priority = priorityPenalty * meeting.RequisitionItem.MeetingTimePriorities.Length * AndreyConstant + 1;
            var priorityText = Math.Abs(priorityPenalty - 1) < 0.01 ? "IGNORED" : $"{(int) priority}-th";
            return $"{priorityText} time priority ({meeting.MeetingTime}) for " +
                   $"{meeting.Discipline} " +
                   $"{meeting.Teacher} " +
                   $"{meeting.MeetingType} " +
                   $"[{meeting.GroupsChoice}]";
        }

        protected override double FindPriorityPenalty(Meeting meeting)
        {
            var meetingTime = meeting.MeetingTime!;
            var priorities = meeting.RequisitionItem.MeetingTimePriorities;
            var prioritiesLength = priorities.Length;
            for (var i = 0; i < prioritiesLength; i++)
                if (priorities[i].MeetingTimeChoices.Contains(meetingTime))
                    return (double) i / prioritiesLength / AndreyConstant;
            return 1;
        }
    }
}