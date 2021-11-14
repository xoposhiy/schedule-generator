using System.Collections.Generic;
using Domain.Enums;
using Domain.MeetingsParts;

namespace Domain.Conversions
{
    public static class RequisitionToMeetingConverter
    {
        private static readonly Dictionary<RequisitionItem, List<Meeting>> Cached = new();
        
        public static List<Meeting> ConvertRequisitionToBaseMeeting(RequisitionItem requisitionItem)
        {
            if (Cached.ContainsKey(requisitionItem))
                return Cached[requisitionItem];
                
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

            Cached[requisitionItem] = meetings;
            return meetings;
        }
    }
}