using System;
using System.Linq;

namespace Domain.Algorithms.Estimators.PriorityEstimators
{
    public class GroupPriorityEstimator : PriorityEstimator
    {
        protected override string GetLogMessage(Meeting meeting, double priorityPenalty)
        {
            var priority = priorityPenalty * meeting.RequisitionItem.GroupPriorities.Length * AndreyConstant + 1;
            var priorityText = Math.Abs(priorityPenalty - 1) < 0.01 ? "IGNORED" : $"{(int) priority}-th";
            return $"{meeting.Discipline} " +
                   $"{meeting.Teacher} " +
                   $"{meeting.MeetingType} has {priorityText} group priority ({meeting.GroupsChoice}) for " +
                   $"[{meeting.GroupsChoice}]";
        }

        protected override double FindPriorityPenalty(Meeting meeting)
        {
            var basicMeeting = meeting.BaseMeeting;
            var priorities = basicMeeting!.RequisitionItem.GroupPriorities;
            var prioritiesLength = priorities.Length;
            for (var i = 0; i < prioritiesLength; i++)
                if (priorities[i].GroupsChoices.Any(gc => meeting.GroupsChoice!.Groups.SequenceEqual(gc.Groups)))
                    return (double) i / prioritiesLength / AndreyConstant;
            return 1;
        }
    }
}