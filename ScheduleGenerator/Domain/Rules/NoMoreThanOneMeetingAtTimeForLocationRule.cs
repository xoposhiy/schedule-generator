using Domain.ScheduleLib;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Rules
{
    public class NoMoreThanOneMeetingAtTimeForLocationRule : IRule
    {
        public double UnitPenalty;

        public NoMoreThanOneMeetingAtTimeForLocationRule(double unitPenalty = double.PositiveInfinity)
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
                "Нельзя проводить более одной пары в одном месте одновременно"
            );
        }

        private Meeting[] GetBadMeetings(Schedule schedule)
        {
            var badMeetings = new List<Meeting>();
            foreach (var grouping in schedule.Meetings.GroupBy(meeting => meeting.Location))
            {
                if(grouping.Key == "Online")
                {
                    continue;
                }
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
