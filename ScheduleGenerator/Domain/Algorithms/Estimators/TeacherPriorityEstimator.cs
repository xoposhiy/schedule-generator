using System;
using Infrastructure;

namespace Domain.Algorithms.Estimators
{
    public class TeacherPriorityEstimator : IEstimator
    {
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
            var priority = priorityPenalty * meeting.RequisitionItem.MeetingTimePriorities.Length * 5 + 1;
            var priorityText = Math.Abs(priorityPenalty - 1) < 0.01 ? "IGNORED" : $"{(int) priority}-th";
            return $"{meeting.Discipline} " +
                   $"{meeting.Teacher} " +
                   $"{meeting.MeetingType} has {priorityText} time priority ({meeting.MeetingTime}) for " +
                   $"[{meeting.GroupsChoice}]";
        }

        private static double FindPriorityPenalty(Meeting meeting)
        {
            var meetingTime = meeting.MeetingTime!;
            var priorities = meeting.RequisitionItem.MeetingTimePriorities;
            var prioritiesLength = priorities.Length;
            for (var i = 0; i < prioritiesLength; i++)
                if (priorities[i].MeetingTimeChoices.Contains(meetingTime))
                    return (double) i / prioritiesLength / 5;

            return 1;
        }
    }
}