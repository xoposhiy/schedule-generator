using System;
using System.Collections.Generic;
using System.Linq;
using CommonDomain.Enums;
using Domain.MeetingsParts;

namespace Domain.Conversions
{
    public static class RequisitionToMeetingConverter
    {
        private static readonly Dictionary<Requisition, List<Meeting>> Cached = new();

        private static List<Meeting> ConvertRequisitionItemToBaseMeeting(RequisitionItem requisitionItem)
        {
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

        public static List<Meeting> ConvertRequisitionToBaseMeeting(this Requisition requisition)
        {
            if (Cached.ContainsKey(requisition))
                return Cached[requisition];
            var meetings = requisition.Items
                .SelectMany(ConvertRequisitionItemToBaseMeeting).ToList();
            LinkBaseMeetings(meetings);
            return Cached[requisition] = meetings;
        }

        private static void LinkBaseMeetings(IReadOnlyCollection<Meeting> notUsedMeetings)
        {
            foreach (var meeting in notUsedMeetings)
            {
                var requiredAdjacentMeetingType = meeting.PlanItem.RequiredAdjacentMeetingType;
                if (requiredAdjacentMeetingType == null) continue;
                if (meeting.RequiredAdjacentMeeting != null) continue;
                var linkedMeeting = notUsedMeetings
                    .Where(m => m.RequiredAdjacentMeeting == null)
                    .Where(m => m.Teacher == meeting.Teacher)
                    .FirstOrDefault(e => e.Discipline.Equals(meeting.Discipline)
                                         && e.MeetingType.Equals(requiredAdjacentMeetingType)
                                         && !ReferenceEquals(e, meeting));
                if (linkedMeeting == null)
                    throw new ArgumentException(meeting.ToString());
                meeting.Link(linkedMeeting);
            }
        }
    }
}