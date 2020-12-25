using System;
using System.Collections.Generic;
using System.Text;
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
                meeting.WeekType = requisition.WeekType;
                meetings.Add(meeting);
            }
            return meetings;
        }
    }
}
