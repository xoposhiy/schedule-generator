using System.Collections.Generic;
using Domain.ScheduleLib;

namespace Domain.Conversions
{
    public static class RequisitionToMeetingConverter
    {
        public static HashSet<Meeting> ConvertRequisitionToMeetingWithoutTime(RequisitionItem requisitionItem)
        {
            var discipline = requisitionItem.PlanItem.Discipline;
            var meetingType = requisitionItem.PlanItem.MeetingType;

            var meetings = new HashSet<Meeting>();
            for (int i = 0; i < requisitionItem.RepetitionsCount; i++)
            {
                var meeting = new Meeting(discipline, meetingType, null);
                meeting.Location = requisitionItem.Location;
                meeting.Teacher = requisitionItem.Teacher;
                // According to meetings per week from learn plan
                meeting.WeekType = requisitionItem.WeekType;
                meetings.Add(meeting);
            }
            return meetings;
        }

        public static (HashSet<Meeting>, AdditionalMeetingInfo) ConvertRequisitionToMeeting(RequisitionItem requisitionItem)
        {
            var meetings = ConvertRequisitionToMeetingWithoutTime(requisitionItem);
            var additionalInfo = new AdditionalMeetingInfo(requisitionItem);
            return (meetings, additionalInfo);
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
            if (requisitionItem.WeekType == WeekType.Any)
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
