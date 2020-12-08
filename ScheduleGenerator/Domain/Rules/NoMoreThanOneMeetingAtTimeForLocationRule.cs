using Domain.ScheduleLib;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Rules
{
    class NoMoreThanOneMeetingAtTimeForLocationRule : IRule
    {
        public double Penalty;

        public NoMoreThanOneMeetingAtTimeForLocationRule(double penalty = double.PositiveInfinity)
        {
            Penalty = penalty;
        }

        public EvaluationResult Evaluate(Schedule schedule)
        {
            var badMeetings = FindBadMeetings(schedule.Meetings);
            return new EvaluationResult
            (
                badMeetings.Length != 0 ? Penalty : 0,
                badMeetings,
                "Нельзя проводить более одной пары в одном месте одновременно"
            );
        }

        private Meeting[] FindBadMeetings(Meeting[] meetings)
        {
            var badMeetings = new List<Meeting>();
            foreach (var grouping in meetings.GroupBy(meeting => meeting.Location))
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
