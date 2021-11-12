using System;
using Infrastructure;

namespace Domain.Algorithms.Estimators
{
    public class TeacherPriorityEstimator : IEstimator
    {
        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            throw new NotImplementedException();
        }

        public double Estimate(Schedule schedule, ILogger? logger = null)
        {
            var penalty = 0d;
            var meetingsCount = schedule.Meetings.Count + schedule.NotUsedMeetings.Count;
            foreach (var meeting in schedule.Meetings)
            {
                var priorityPenalty = FindPriorityPenalty(meeting);
                if (priorityPenalty == 0) continue;
                var penaltyPart = priorityPenalty / meetingsCount;
                logger?.Log(GetLogMessage(meeting, priorityPenalty), -penaltyPart);
                penalty += penaltyPart;
            }

            return -penalty;
        }

        private static string GetLogMessage(Meeting meeting, double priorityPenalty)
        {
            var priority = priorityPenalty * meeting.RequisitionItem.MeetingTimePriorities.Length + 1;
            return $"{meeting.Discipline} " +
                   $"{meeting.Teacher} " +
                   $"{meeting.MeetingType} has {(int) priority}-th time priority ({meeting.MeetingTime}) for " +
                   $"[{meeting.GroupsChoice}]";
        }

        private static double FindPriorityPenalty(Meeting meeting)
        {
            var meetingTime = meeting.MeetingTime!;
            var priorities = meeting.RequisitionItem.MeetingTimePriorities;
            var prioritiesLength = priorities.Length;
            for (var i = 0; i < prioritiesLength; i++)
                if (priorities[i].MeetingTimeChoices.Contains(meetingTime))
                    return (double) i / prioritiesLength / 2;

            return 1;
        }
    }
}