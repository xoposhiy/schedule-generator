using System.Collections.Generic;
using Domain.ScheduleLib;

namespace Domain.Rules
{
    public class NoMoreThanOneMeetingAtTimeForTeacherRule : IRule
    {
        public readonly double UnitPenalty;

        public NoMoreThanOneMeetingAtTimeForTeacherRule(double unitPenalty = 1500)
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
            foreach (var frozenMeeting in schedule.Meetings)
            {
                if ((frozenMeeting.WeekType == meetingToAdd.WeekType
                    || frozenMeeting.WeekType == WeekType.Any
                    || meetingToAdd.WeekType == WeekType.Any) &&
                        frozenMeeting.Teacher.Equals(meetingToAdd.Teacher) &&
                        frozenMeeting.MeetingTime.Equals(meetingToAdd.MeetingTime) &&
                        !frozenMeeting.Discipline.Equals(meetingToAdd.Discipline) &&
                        frozenMeeting.MeetingType == meetingToAdd.MeetingType &&
                        frozenMeeting.MeetingType == MeetingType.Lecture)
                {
                    return new List<Meeting>() { frozenMeeting };
                }
            }

            return new List<Meeting>();
        }
    }
}
