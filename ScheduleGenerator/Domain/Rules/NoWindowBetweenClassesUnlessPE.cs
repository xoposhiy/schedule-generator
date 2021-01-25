using System.Collections.Generic;
using System.Linq;
using Domain.ScheduleLib;

namespace Domain.Rules
{
    class NoWindowBetweenClassesUnlessPE : IRule
    {
        public readonly double UnitPenalty;
        public List<MeetingTime> peClasses;

        public NoWindowBetweenClassesUnlessPE(double unitPenalty = 1500, List<MeetingTime> peClasses = null)
        {
            UnitPenalty = unitPenalty;
            this.peClasses = peClasses is null ? new List<MeetingTime>() : peClasses;
        }

        public double Evaluate(LearningPlan learningPlan, Requisition[] requisition, Schedule schedule, Meeting meetingToAdd)
        {
            var windowDifference = FindWindowDifference(schedule, meetingToAdd);
            if (windowDifference < 0)
            {
                return 0;
            }
            var totalPenalty = UnitPenalty * windowDifference;
            return totalPenalty;
        }

        public static int FindWindowDifference(Schedule schedule, Meeting meetingToAdd)
        {
            //var todaysPEClass = peClasses.Where(m => m.Day == day).FirstOrDefault();
            var todaysMeetings = schedule.Meetings
                //.Where(m => m.WeekType == meetingToAdd.WeekType)
                .Where(m => m.WeekType == meetingToAdd.WeekType || m.WeekType == WeekType.Any || meetingToAdd.WeekType == WeekType.Any)
                .Where(m => m.MeetingTime.Day == meetingToAdd.MeetingTime.Day)
                .Where(m => m.Groups.First() == meetingToAdd.Groups.First());

            var windowCountInSchedule = FindWindowCount(todaysMeetings);

            var todaysMeetingsSet = todaysMeetings.ToHashSet();
            todaysMeetingsSet.Add(meetingToAdd);
            var windowCountInNewSchedule = FindWindowCount(todaysMeetings);

            var difference = windowCountInNewSchedule - windowCountInSchedule;

            return difference;
        }

        private static int FindMaxWindow(IEnumerable<Meeting> meetings)
        {
            var orderedMeetings = meetings.OrderBy(m => m.MeetingTime.TimeSlotIndex);
            var lastPointer = meetings.First().MeetingTime.TimeSlotIndex;
            var maxWindow = 0;

            foreach (var meeting in orderedMeetings)
            {
                var currentPointer = meeting.MeetingTime.TimeSlotIndex;
                var currentWindow = currentPointer - lastPointer;
                if (currentWindow > maxWindow)
                {
                    maxWindow = currentWindow;
                }
                lastPointer = currentPointer;
            }

            return maxWindow;
        }

        private static int FindWindowCount(IEnumerable<Meeting> meetings)
        {
            var orderedMeetings = meetings.OrderBy(m => m.MeetingTime.TimeSlotIndex);
            var lastPointer = meetings.First().MeetingTime.TimeSlotIndex;

            var windowCount = 0;

            foreach (var meeting in orderedMeetings)
            {
                var currentPointer = meeting.MeetingTime.TimeSlotIndex;
                var currentWindow = currentPointer - lastPointer;
                if (currentWindow > 1)
                {
                    windowCount++;
                }
                lastPointer = currentPointer;
            }

            return windowCount;
        }
    }
}
