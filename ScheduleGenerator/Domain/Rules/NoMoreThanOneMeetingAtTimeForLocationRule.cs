using System.Collections.Generic;
using Domain.ScheduleLib;

namespace Domain.Rules
{
    public class NoMoreThanOneMeetingAtTimeForLocationRule : IRule
    {
        public readonly double UnitPenalty;

        public NoMoreThanOneMeetingAtTimeForLocationRule(double unitPenalty = 1500)
        {
            UnitPenalty = unitPenalty;
        }

        public double Evaluate(LearningPlan learningPlan, Requisition requisition, Schedule schedule, Meeting meetingToAdd)
        {
            var badMeetings = GetCollidedMeetings(schedule, meetingToAdd);
            var totalPenalty = UnitPenalty * badMeetings.Count;
            return totalPenalty;
        }

        public static List<Meeting> GetCollidedMeetings(Schedule schedule, Meeting meetingToAdd)
        {
            if (meetingToAdd.Location.ToLower() == "online")
            {
                return new List<Meeting>();
            }

            foreach (var frozenMeeting in schedule.Meetings)
            {
                if ((frozenMeeting.WeekType == meetingToAdd.WeekType
                    || frozenMeeting.WeekType == WeekType.All
                    || meetingToAdd.WeekType == WeekType.All) &&
                        frozenMeeting.Location == meetingToAdd.Location &&
                        frozenMeeting.MeetingTime.Equals(meetingToAdd.MeetingTime) &&
                        !(frozenMeeting.Discipline.Equals(meetingToAdd.Discipline) &&
                          frozenMeeting.Teacher.Equals(meetingToAdd.Teacher)))
                {
                    return new List<Meeting>() { frozenMeeting };
                }
            }

            return new List<Meeting>();
        }
    }
}
