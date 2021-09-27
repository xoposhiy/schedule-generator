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
            var teacher = requisitionItem.Teacher;

            var meetings = new List<Meeting>();
            
            var meetingCount = requisitionItem.RepetitionsCount * (int) requisitionItem.PlanItem.MeetingsPerWeek;
            if (requisitionItem.PlanItem.MeetingsPerWeek % 1 == 0)
            {
                for (var i = 0; i < meetingCount; i++)
                {
                    meetings.Add(new Meeting(discipline, meetingType, teacher, requisitionItem.WeekType,
                        requisitionItem));
                }
            }
            else
            {
                for (var i = 0; i < meetingCount; i++)
                {
                    meetings.Add(new Meeting(discipline, meetingType, teacher, WeekType.All, requisitionItem));
                }

                var weekType = requisitionItem.WeekType != WeekType.All
                    ? requisitionItem.WeekType
                    : WeekType.OddOrEven;
                for (var i = 0; i < requisitionItem.RepetitionsCount; i++)
                {
                    meetings.Add(new Meeting(discipline, meetingType, teacher, weekType, requisitionItem));
                }
            }

            // Console.WriteLine(string.Join(" ", meetings));
            return meetings;
        }
    }

    public class AdditionalMeetingInfo
    {
        public List<HashSet<HashSet<MeetingGroup>>> PossibleGroups;

        public HashSet<WeekType> PossibleWeekType;

        //public List<HashSet<DayOfWeek>> possibleDays;
        //public List<HashSet<int>> possibleIndexes;
        public List<HashSet<MeetingTime>> PossibleMeetingTimes;

        public AdditionalMeetingInfo(RequisitionItem requisitionItem)
        {
            // groups
            PossibleGroups = new List<HashSet<HashSet<MeetingGroup>>>();
            foreach (var groupRequisition in requisitionItem.GroupPriorities)
            {
                PossibleGroups.Add(new HashSet<HashSet<MeetingGroup>>());
                foreach (var groupChoice in groupRequisition.GroupsChoices)
                {
                    PossibleGroups[^1].Add(
                        new HashSet<MeetingGroup>(groupChoice.Groups));
                }
            }

            // week type (even/odd)
            if (requisitionItem.WeekType == WeekType.All)
            {
                PossibleWeekType = new HashSet<WeekType> {WeekType.Even, WeekType.Odd};
            }
            else
            {
                PossibleWeekType = new HashSet<WeekType> {requisitionItem.WeekType};
            }

            // week days
            PossibleMeetingTimes = new List<HashSet<MeetingTime>>();
            foreach (var meetingTimeRequisition in requisitionItem.MeetingTimePriorities)
            {
                PossibleMeetingTimes.Add(new HashSet<MeetingTime>(meetingTimeRequisition.MeetingTimeChoices));
            }
        }
    }
}