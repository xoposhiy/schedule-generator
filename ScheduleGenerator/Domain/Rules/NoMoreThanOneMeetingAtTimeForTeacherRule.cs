using Domain.ScheduleLib;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Rules
{
    public class NoMoreThanOneMeetingAtTimeForTeacherRule : IRule
    {
        public double UnitPenalty;

        public NoMoreThanOneMeetingAtTimeForTeacherRule(double unitPenalty = double.PositiveInfinity)
        {
            UnitPenalty = unitPenalty;
        }

        public EvaluationResult Evaluate(Schedule schedule, Requisition requisition)
        {
            var badMeetings = GetBadMeetings(schedule);
            return new EvaluationResult
            (
                badMeetings.Length > 0 ? UnitPenalty : 0,
                badMeetings,
                "Преподаватель не должен проводить более одной пары одновременно"
            );
        }

        private Meeting[] GetBadMeetings(Schedule schedule)
        {
            var badMeetings = new List<Meeting>();
            foreach (var grouping in schedule.Meetings.GroupBy(meeting => meeting.Teacher))
            {
                var sortedByTimeMeetings = new Dictionary<MeetingTime, List<Meeting>>();
                foreach (var meeting in grouping)
                {
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
                        badMeetings.Concat(sameTimeMeetings);
                    }
                }
            }
            return badMeetings.ToArray();
        }
    }
}
