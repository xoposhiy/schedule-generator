using System;
using System.Linq;
using Domain.MeetingsParts;
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
            var meetingsCount = schedule.Meetings.Count;
            foreach (var meeting in schedule.Meetings)
            {
                var meetingTime = meeting.MeetingTime;
                var priorities = meeting.RequisitionItem.MeetingTimePriorities;
                var prioritiesLength = priorities.Length;
                for (var i = 0; i < prioritiesLength; i++)
                {
                    if (!priorities[i].MeetingTimeChoices.Contains(meetingTime)) continue;
                    var penaltyPart = (double) i / (prioritiesLength * meetingsCount);
                    penalty += penaltyPart;
                    if (i > 0)
                        logger?.Log(GetLogMessage(meeting, i + 1), -penaltyPart);
                    break;
                }
            }

            return -penalty;
        }

        private static string GetLogMessage(Meeting meeting, int priority)
        {
            return $"{meeting.Discipline} " +
                   $"{meeting.Teacher} " +
                   $"{meeting.MeetingType} has {priority}-th time priority ({meeting.MeetingTime}) for " +
                   $"[{string.Join<MeetingGroup>(" ", meeting.Groups!)}]";
        }
    }
}