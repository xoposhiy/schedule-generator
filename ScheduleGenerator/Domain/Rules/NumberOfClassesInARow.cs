using System.Collections.Generic;
using System.Linq;
using Domain.ScheduleLib;

namespace Domain.Rules
{
    public class NumberOfClassesInARow : IRule
    {
        public readonly double UnitPenalty;
        public int MaxClassesInARow { get; set; }

        public NumberOfClassesInARow(double unitPenalty = 5, int maxClassesInARow = 5)
        {
            UnitPenalty = unitPenalty;
            MaxClassesInARow = maxClassesInARow;
        }

        public double Evaluate(LearningPlan learningPlan, Requisition requisition, Schedule schedule, Meeting meetingToAdd)
        {
            var classesInARowDifference = FindClassesInARowDifference(schedule, meetingToAdd);
            if (classesInARowDifference < 0)
            {
                return 0;
            }
            var totalPenalty = UnitPenalty * classesInARowDifference;
            return totalPenalty;
        }

        private int FindClassesInARowDifference(Schedule schedule, Meeting meetingToAdd)
        {
            var todaysMeetings = schedule.Meetings
                .Where(m => m.WeekType == meetingToAdd.WeekType || m.WeekType == WeekType.All || meetingToAdd.WeekType == WeekType.All)
                .Where(m => m.MeetingTime.Day == meetingToAdd.MeetingTime.Day)
                .Where(m => m.Groups.First().Equals(meetingToAdd.Groups.First()));
            if (!todaysMeetings.Any())
            {
                return 0;
            }

            var maxCountInSchedule = FindMaxClassesInARow(todaysMeetings);
            if (maxCountInSchedule < MaxClassesInARow)
            {
                return 0;
            }
            var newTodaysMeetings = todaysMeetings.ToHashSet();
            newTodaysMeetings.Add(meetingToAdd);
            var maxCountInNewSchedule = FindMaxClassesInARow(newTodaysMeetings);
            return maxCountInNewSchedule - maxCountInSchedule;
        }

        private static int FindMaxClassesInARow(IEnumerable<Meeting> meetings)
        {
            var orderedMeetings = meetings.OrderBy(m => m.MeetingTime.TimeSlotIndex);

            var lastPointer = orderedMeetings.First().MeetingTime.TimeSlotIndex;
            var currentPointer = lastPointer;
            var meetingCountInARow = 0;
            var maxCount = 0;
            foreach (var meeting in orderedMeetings)
            {
                currentPointer = meeting.MeetingTime.TimeSlotIndex;
                if (currentPointer - lastPointer < 2)
                {
                    meetingCountInARow++;
                    if (meetingCountInARow > maxCount)
                    {
                        maxCount = meetingCountInARow;
                    }
                }
                else
                {
                    meetingCountInARow = 1;
                }
                lastPointer = currentPointer;
            }

            return maxCount;
        }
    }
}
