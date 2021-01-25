using System.Collections.Generic;
using System.Linq;
using Domain.ScheduleLib;

namespace Domain.Rules
{
    public class NoMoreThanOneMeetingAtTimeForGroupRule
    {
        public readonly double UnitPenalty;

        public NoMoreThanOneMeetingAtTimeForGroupRule(double unitPenalty = 1500)
        {
            UnitPenalty = unitPenalty;
        }

        public double Evaluate(LearningPlan learningPlan, Requisition[] requisition, Schedule schedule, Meeting meetingToAdd)
        {
            var badMeetings = GetCollidedMeetings(schedule, meetingToAdd);
            var totalPenalty = UnitPenalty * badMeetings.Count;
            return totalPenalty;
        }

        public static List<Meeting> GetCollidedMeetings(Schedule schedule, Meeting meetingToAdd)
        {
            var meetingsWithSameGroup = schedule.Meetings
                .Where(m => m.Groups.First().Equals(meetingToAdd.Groups.First()))
                .Where(m => m.WeekType == meetingToAdd.WeekType || m.WeekType == WeekType.Any || meetingToAdd.WeekType == WeekType.Any)
                .Where(m => m.MeetingTime.Equals(meetingToAdd.MeetingTime))
                .ToList();

            return meetingsWithSameGroup;
        }
    }
}
