using System.Collections.Generic;
using Domain.Enums;
using Domain.MeetingsParts;

namespace Domain.Conversions
{
    public static class RequisitionToMeetingConverter
    {
        public static List<Meeting> ConvertRequisitionToBasicMeeting(RequisitionItem requisitionItem)
        {
            var meetings = new List<Meeting>();

            var meetingCount = requisitionItem.RepetitionsCount * (int) requisitionItem.PlanItem.MeetingsPerWeek;
            if (requisitionItem.PlanItem.MeetingsPerWeek % 1 == 0)
            {
                for (var i = 0; i < meetingCount; i++)
                    meetings.Add(new(requisitionItem.WeekType,
                        requisitionItem));
            }
            else
            {
                for (var i = 0; i < meetingCount; i++)
                    meetings.Add(new(WeekType.All, requisitionItem));

                var weekType = requisitionItem.WeekType != WeekType.All
                    ? requisitionItem.WeekType
                    : WeekType.OddOrEven;
                for (var i = 0; i < requisitionItem.RepetitionsCount; i++)
                    meetings.Add(new(weekType, requisitionItem));
            }

            // Console.WriteLine(string.Join(" ", meetings));
            return meetings;
        }
    }
}