using System;
using System.Linq;
using Infrastructure;

namespace Domain.Algorithms.Estimators
{
    public class GroupPriorityEstimator : IEstimator
    {
        private const double AndreyConstant = 5;
        // private const double DanyaConstant = Math.E * 100;


        public double EstimateMeetingToAdd(Schedule schedule, Meeting meetingToAdd)
        {
            var maxPenalty = schedule.Meetings.Count + schedule.NotUsedMeetings.Count;
            var penaltyDelta = 0d;

            foreach (var linkedMeeting in meetingToAdd.GetLinkedMeetings())
                penaltyDelta += FindPriorityPenalty(linkedMeeting);

            return -penaltyDelta / maxPenalty;
        }

        public double Estimate(Schedule schedule, ILogger? logger = null)
        {
            var penalty = 0d;
            var maxPenalty = schedule.Meetings.Count + schedule.NotUsedMeetings.Count;
            foreach (var meeting in schedule.Meetings)
            {
                var priorityPenalty = FindPriorityPenalty(meeting);
                if (priorityPenalty == 0) continue;
                logger?.Log(GetLogMessage(meeting, priorityPenalty), -priorityPenalty / maxPenalty);
                penalty += priorityPenalty;
            }

            return -penalty / maxPenalty;
        }

        private static string GetLogMessage(Meeting meeting, double priorityPenalty)
        {
            var priority = priorityPenalty * meeting.RequisitionItem.GroupPriorities.Length * AndreyConstant + 1;
            var priorityText = Math.Abs(priorityPenalty - 1) < 0.01 ? "IGNORED" : $"{(int) priority}-th";
            return $"{meeting.Discipline} " +
                   $"{meeting.Teacher} " +
                   $"{meeting.MeetingType} has {priorityText} group priority ({meeting.GroupsChoice}) for " +
                   $"[{meeting.GroupsChoice}]";
        }

        private static double FindPriorityPenalty(Meeting meeting)
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