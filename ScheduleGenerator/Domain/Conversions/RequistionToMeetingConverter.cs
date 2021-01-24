using System;
using System.Collections.Generic;
using System.Linq;
using Domain.ScheduleLib;


namespace Domain.Conversions
{
    public static class RequistionToMeetingConverter
    {
        public static HashSet<Meeting> ConvertRequistionToMeetingWithoutTime(Requisition requisition)
        {
            var discipline = requisition.PlanItem.Discipline;
            var meetingType = requisition.PlanItem.MeetingType;

            var meetings = new HashSet<Meeting>();
            for (int i = 0; i < requisition.RepetitionsCount; i++)
            {
                var meeting = new Meeting(discipline, meetingType, null);
                meeting.Location = requisition.Location;
                meeting.Teacher = requisition.Teacher;
                // According to meetings per week from learn plan
                meeting.WeekType = requisition.WeekType;
                meetings.Add(meeting);
            }
            return meetings;
        }

        public static (HashSet<Meeting>, AdditionalMeetingInfo) ConvertRequistionToMeeting(Requisition requisition)
        {
            var meetings = ConvertRequistionToMeetingWithoutTime(requisition);
            var additionalInfo = new AdditionalMeetingInfo(requisition);
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
        public AdditionalMeetingInfo(Requisition requisition)
        {
            // groups
            possibleGroups = new List<HashSet<HashSet<MeetingGroup>>>();
            foreach (var groupRequisition in requisition.GroupPriorities)
            {
                possibleGroups.Add(new HashSet<HashSet<MeetingGroup>>());
                foreach (var groupChoice in groupRequisition.GroupsChoices)
                {
                    possibleGroups[possibleGroups.Count - 1].Add(
                        new HashSet<MeetingGroup>(groupChoice.Groups));
                }
            }

            // week type (even/odd)
            if (requisition.WeekType == WeekType.Any)
            {
                possibleWeekType = new HashSet<WeekType>() { WeekType.Even, WeekType.Odd };
            }
            else
            {
                possibleWeekType = new HashSet<WeekType>() { requisition.WeekType };
            }

            // week days
            possibleMeetingTimes = new List<HashSet<MeetingTime>>();
            foreach (var meetingTimeRequestion in requisition.MeetingTimePriorities)
            {
                possibleMeetingTimes.Add(new HashSet<MeetingTime>(meetingTimeRequestion.MeetingTimeChoices));
            }
        }
    }
}
