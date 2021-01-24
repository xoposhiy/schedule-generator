using System;
using Domain.ScheduleLib;
using System.Collections.Generic;
using System.Linq;

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

        public double Evaluate(LearningPlan learningPlan, Requisition requisition, Schedule schedule, Meeting meetingToAdd)
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
                .Where(m => m.WeekType == meetingToAdd.WeekType)
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

        //public EvaluationResult Evaluate(Schedule schedule, Requisition requisition)
        //{
        //    var badMeetings = GetBadMeetings(schedule);
        //    return new EvaluationResult
        //    (
        //        badMeetings.Length * UnitPenalty,
        //        badMeetings,
        //        "Желательно избавляться от окон между парами, только если это не физкультура"
        //    );
        //}

        //private Meeting[] GetBadMeetings(Schedule schedule)
        //{
        //    var badMeetings = new List<Meeting>();
        //    foreach (var weekTypeGrouping in schedule.Meetings.GroupBy(m => m.WeekType))
        //    {
        //        // Should be exactly one group specified in meeting
        //        foreach (var groupGrouping in weekTypeGrouping.GroupBy(m => m.Groups.First()))
        //        {
        //            foreach (var dayGrouping in groupGrouping.GroupBy(m => m.MeetingTime.Day))
        //            {
        //                var currentBadMeetings = AnalizeDay((IEnumerable<Meeting>)dayGrouping.GetEnumerator(), dayGrouping.Key);
        //                badMeetings.AddRange(currentBadMeetings);
        //            }
        //        }
        //    }
        //    return badMeetings.ToArray();
        //}

        //private IEnumerable<Meeting> AnalizeDay(IEnumerable<Meeting> meetings, DayOfWeek day)
        //{
        //    if (!meetings.Any())
        //    {
        //        return meetings;
        //    }
        //    var todaysBadMeetings = new List<Meeting>();
        //    var todaysPEClass = peClasses.Where(m => m.Day == day).FirstOrDefault();

        //    var orderedMeetings = meetings.OrderBy(m => m.MeetingTime.TimeSlotIndex);
        //    var lastMeeting = orderedMeetings.First();
        //    var lastPointer = lastMeeting.MeetingTime.TimeSlotIndex;
        //    var currentPointer = lastPointer;

        //    foreach (var meeting in orderedMeetings)
        //    {
        //        currentPointer = meeting.MeetingTime.TimeSlotIndex;
        //        if (currentPointer - lastPointer > 1)
        //        {
        //            if (!(todaysPEClass != null &&
        //                    todaysPEClass.TimeSlotIndex - lastPointer <= 2 &&
        //                    currentPointer - todaysPEClass.TimeSlotIndex <= 2))
        //            {
        //                // bad meetings
        //                todaysBadMeetings.Add(lastMeeting);
        //                todaysBadMeetings.Add(meeting);
        //            }
        //        }
        //        lastMeeting = meeting;
        //        lastPointer = currentPointer;
        //    }

        //    return todaysBadMeetings;
        //}
    }
}
