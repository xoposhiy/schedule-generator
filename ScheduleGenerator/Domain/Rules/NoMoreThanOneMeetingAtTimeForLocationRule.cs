using Domain.ScheduleLib;
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
                if (frozenMeeting.WeekType == meetingToAdd.WeekType &&
                        frozenMeeting.Location == meetingToAdd.Location &&
                        frozenMeeting.MeetingTime == meetingToAdd.MeetingTime)
                {
                    return new List<Meeting>() { frozenMeeting };
                }
            }

            return new List<Meeting>();
        }

        //public EvaluationResult Evaluate(Schedule schedule, Requisition requisition)
        //{
        //    var badMeetings = GetBadMeetings(schedule);
        //    return new EvaluationResult
        //    (
        //        badMeetings.Length > 0 ? UnitPenalty : 0,
        //        badMeetings,
        //        "Нельзя проводить несколько пар в одном месте одновременно"
        //    );
        //}

        //private static Meeting[] GetBadMeetings(Schedule schedule)
        //{
        //    var badMeetings = new List<Meeting>();
        //    foreach (var grouping in schedule.Meetings.GroupBy(meeting => meeting.Location))
        //    {
        //        if(grouping.Key == "Online")
        //        {
        //            continue;
        //        }
        //        var sortedByTimeMeetings = new Dictionary<MeetingTime, List<Meeting>>();
        //        foreach (var meeting in grouping)
        //        {
        //            if (meeting.MeetingTime is null)
        //            {
        //                continue;
        //            }
        //            if (!sortedByTimeMeetings.ContainsKey(meeting.MeetingTime))
        //            {
        //                sortedByTimeMeetings[meeting.MeetingTime] = new List<Meeting>();
        //            }
        //            sortedByTimeMeetings[meeting.MeetingTime].Add(meeting);
        //        }
        //        foreach (var sameTimeMeetings in sortedByTimeMeetings.Values)
        //        {
        //            if (sameTimeMeetings.Count > 1)
        //            {
        //                badMeetings = badMeetings.Concat(sameTimeMeetings).ToList();
        //            }
        //        }
        //    }
        //    return badMeetings.ToArray();
        //}
    }
}
