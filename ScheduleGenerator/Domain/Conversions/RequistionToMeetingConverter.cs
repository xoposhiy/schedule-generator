using System;
using System.Collections.Generic;
using System.Text;
using Domain.ScheduleLib;


namespace Domain.Conversions
{
    public static class RequistionToMeetingConverter
    {
        public static List<HashSet<Meeting>> ConvertRequistionToMeetingWithoutTime(Requisition requisition)
        {
            var discipline = requisition.PlanItem.Discipline;
            var meetingType = requisition.PlanItem.MeetingType;

            var meetingByPriorityList = new List<HashSet<Meeting>>();
            foreach (var groupRequisition in requisition.GroupPriorities)
            {
                var meetingsOfPriority = new HashSet<Meeting>();
                foreach (var groupChoices in groupRequisition.GroupsChoices)
                {
                    for (int i = 0; i < requisition.RepetitionsCount; i++)
                    {
                        var meeting = new Meeting(discipline, meetingType, groupChoices.Groups);
                        meeting.Location = requisition.Location;
                        meetingsOfPriority.Add(meeting);
                    }
                }
                meetingByPriorityList.Add(meetingsOfPriority);
            }
            return meetingByPriorityList;
        }
    }
}
