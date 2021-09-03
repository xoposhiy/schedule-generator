using System;
using System.Collections.Generic;
using Domain.ScheduleLib;

namespace Domain.Conversions
{
    public static class RequisitionToMeetingConverter
    {
        public static List<Meeting> ConvertRequisitionToBasicMeeting(RequisitionItem requisitionItem)
        {
            var discipline = requisitionItem.PlanItem.Discipline;
            var meetingType = requisitionItem.PlanItem.MeetingType;
            var meetingTeacher = requisitionItem.Teacher;

            var meetings = new List<Meeting>();

            if (requisitionItem.PlanItem.MeetingsPerWeek % 1 == 0)
            {
                var meetingCount = requisitionItem.RepetitionsCount * requisitionItem.PlanItem.MeetingsPerWeek;
                for (int i = 0; i < meetingCount; i++)
                {
                    meetings.Add(new Meeting(discipline, meetingType, meetingTeacher, requisitionItem.WeekType,
                        requisitionItem, null));
                }
            }
            else
            {
                var meetingCount = requisitionItem.RepetitionsCount * (int)requisitionItem.PlanItem.MeetingsPerWeek;
                for (int i = 0; i < meetingCount; i++)
                {
                    meetings.Add(new Meeting(discipline, meetingType, meetingTeacher, WeekType.All,
                        requisitionItem, null));
                }
                var weekType = requisitionItem.WeekType != WeekType.All 
                    ? requisitionItem.WeekType 
                    : WeekType.OddOrEven;
                for (int i = 0; i < requisitionItem.RepetitionsCount; i++)
                {
                    meetings.Add(new Meeting(discipline, meetingType, meetingTeacher, weekType,
                        requisitionItem, null));
                }
            }
            // Console.WriteLine(string.Join(" ", meetings));
            return meetings;
        }
    }

    public class AdditionalMeetingInfo
    {
        public List<HashSet<HashSet<MeetingGroup>>> possibleGroups;
        public HashSet<WeekType> possibleWeekType;
        //public List<HashSet<DayOfWeek>> possibleDays;
        //public List<HashSet<int>> possibleIndexes;
        public List<HashSet<MeetingTime>> possibleMeetingTimes;
        public AdditionalMeetingInfo(RequisitionItem requisitionItem)
        {
            // groups
            possibleGroups = new List<HashSet<HashSet<MeetingGroup>>>();
            foreach (var groupRequisition in requisitionItem.GroupPriorities)
            {
                possibleGroups.Add(new HashSet<HashSet<MeetingGroup>>());
                foreach (var groupChoice in groupRequisition.GroupsChoices)
                {
                    possibleGroups[possibleGroups.Count - 1].Add(
                        new HashSet<MeetingGroup>(groupChoice.Groups));
                }
            }

            // week type (even/odd)
            if (requisitionItem.WeekType == WeekType.All)
            {
                possibleWeekType = new HashSet<WeekType>() { WeekType.Even, WeekType.Odd };
            }
            else
            {
                possibleWeekType = new HashSet<WeekType>() { requisitionItem.WeekType };
            }

            // week days
            possibleMeetingTimes = new List<HashSet<MeetingTime>>();
            foreach (var meetingTimeRequisition in requisitionItem.MeetingTimePriorities)
            {
                possibleMeetingTimes.Add(new HashSet<MeetingTime>(meetingTimeRequisition.MeetingTimeChoices));
            }
        }
    }
}
