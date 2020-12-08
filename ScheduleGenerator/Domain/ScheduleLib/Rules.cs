using System.Collections.Generic;
using System.Linq;

namespace Domain.ScheduleLib
{
    public class NoMoreThanOneMeetingAtTimeForTeacherRule : IRule
    {
        public double Penalty;

        public NoMoreThanOneMeetingAtTimeForTeacherRule(double penalty = double.PositiveInfinity)
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
                "Преподаватель не должен проводить более одной пары одновременно"
            );
        }

        private Meeting[] FindBadMeetings(Meeting[] meetings)
        {
            var badMeetings = new List<Meeting>();
            foreach (var group in meetings.GroupBy(meeting => meeting.Teacher))
            {
                var sortedByTimeMeetings = new Dictionary<MeetingTime, List<Meeting>>();
                foreach (var meeting in group)
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
