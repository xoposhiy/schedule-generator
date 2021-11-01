using System;
using System.Linq;
using Domain.MeetingsParts;
using Infrastructure;
using static Domain.Extensions;

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
                for (int i = 0; i < prioritiesLength; i++)
                {
                    if (!priorities[i].MeetingTimeChoices.Contains(meetingTime)) continue;
                    var penaltyPart = (double) i / (prioritiesLength * meetingsCount);
                    penalty += penaltyPart;
                    if (i > 0)
                        logger?.Log($"{meeting.Discipline} {string.Join<MeetingGroup>(" ", meeting.Groups!)}" +
                                    $" {meeting.MeetingType} has {i+1}-th time priority ({meetingTime}) for {meeting.Teacher}",
                            -penaltyPart);
                    break;
                }
            }
            return -penalty;
        }
    }
}