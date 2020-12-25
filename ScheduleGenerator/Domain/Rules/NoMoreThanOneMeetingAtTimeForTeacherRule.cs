using Domain.ScheduleLib;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Rules
{
    public class NoMoreThanOneMeetingAtTimeForTeacherRule : IRule
    {
        public readonly double UnitPenalty;

        public NoMoreThanOneMeetingAtTimeForTeacherRule(double unitPenalty = 1500)
        {
            UnitPenalty = unitPenalty;
        }

        public EvaluationResult Evaluate(Schedule schedule, Requisition requisition)
        {
            var badMeetings = GetBadMeetings(schedule);
            return new EvaluationResult
            (
                badMeetings.Length * UnitPenalty,
                badMeetings,
                "Преподаватель не может проводить более чем одну пару одновременно"
            );
        }

        private static Meeting[] GetBadMeetings(Schedule schedule)
        {
            var badMeetings = new List<Meeting>();
            foreach (var grouping in schedule.Meetings.GroupBy(meeting => meeting.Teacher))
            {
                var sortedByTimeMeetings = new Dictionary<MeetingTime, List<Meeting>>();
                foreach (var meeting in grouping)
                {
                    if (meeting.MeetingTime is null)
                    {
                        continue;
                    }
                    if (!sortedByTimeMeetings.ContainsKey(meeting.MeetingTime))
                    {
                        sortedByTimeMeetings[meeting.MeetingTime] = new List<Meeting>();
                    }
                    sortedByTimeMeetings[meeting.MeetingTime].Add(meeting);
                }
                foreach (var sameTimeMeetings in sortedByTimeMeetings.Values)
                {
                    if (sameTimeMeetings.Count > 1)
                    {
                        badMeetings = badMeetings.Concat(sameTimeMeetings).ToList();
                    }
                }
            }
            return badMeetings.ToArray();
        }
    }
}
