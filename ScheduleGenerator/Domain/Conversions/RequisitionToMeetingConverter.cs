using System.Collections.Generic;
using Domain.Enums;
using Domain.MeetingsParts;

namespace Domain.Conversions
{
    public static class RequisitionToMeetingConverter
    {
        public static List<Meeting> ConvertRequisitionToBaseMeeting(RequisitionItem requisitionItem)
        {
            // TODO krutovsky: fix this evil method [three same for-loop - puke]
            var meetings = new List<Meeting>();

            var meetingCount = requisitionItem.RepetitionsCount * (int) requisitionItem.PlanItem.MeetingsPerWeek;

            for (var i = 0; i < meetingCount; i++)
                meetings.Add(new(WeekType.All, requisitionItem));

            if (requisitionItem.PlanItem.MeetingsPerWeek % 1 != 0)
            {
                var weekType = requisitionItem.WeekType != WeekType.All
                    ? requisitionItem.WeekType
                    : WeekType.OddOrEven;
                for (var i = 0; i < requisitionItem.RepetitionsCount; i++)
                    meetings.Add(new(weekType, requisitionItem));
            }

            return meetings;
        }
    }
}