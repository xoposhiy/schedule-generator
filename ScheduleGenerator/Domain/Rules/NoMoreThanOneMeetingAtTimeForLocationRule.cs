﻿using Domain.ScheduleLib;
using System.Collections.Generic;
using System.Linq;

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
                    || frozenMeeting.WeekType == WeekType.Any
                    || meetingToAdd.WeekType == WeekType.Any) &&
                        frozenMeeting.Location == meetingToAdd.Location &&
                        frozenMeeting.MeetingTime == meetingToAdd.MeetingTime)
                {
                    return new List<Meeting>() { frozenMeeting };
                }
            }

            return new List<Meeting>();
        }
    }
}
