using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Domain.Algorithms;
using Domain.Conversions;

namespace Domain.ScheduleLib
{
    public class RequisitionItem
    {
        public readonly LearningPlanItem PlanItem;
        public readonly GroupRequisition[] GroupPriorities;
        public readonly bool IsOnline;
        
        public readonly int RepetitionsCount;

        public readonly MeetingTimeRequisition[] MeetingTimePriorities;
        public readonly Teacher Teacher;
        public readonly WeekType WeekType;

        public RequisitionItem(LearningPlanItem planItem, string groupPriorities,
            string meetingTimePriorities, Teacher teacher, int repetitionsCount = 1, WeekType weekType = WeekType.All, bool isOnline = false)
            : this(planItem,
                SheetToRequisitionConverter.ParseGroupRequisitions(groupPriorities).ToArray(), 
                repetitionsCount,
                SheetToRequisitionConverter.ParseMeetingTimeRequisitions(meetingTimePriorities).ToArray(),
                teacher, weekType, isOnline)
        {
        }
        
        public RequisitionItem(LearningPlanItem planItem, GroupRequisition[] groupPriorities, int repetitionsCount,
            MeetingTimeRequisition[] meetingTimePriorities, Teacher teacher, WeekType weekType = WeekType.All, bool isOnline = false)
        {
            PlanItem = planItem;
            GroupPriorities = groupPriorities;
            RepetitionsCount = repetitionsCount;
            MeetingTimePriorities = meetingTimePriorities;
            Teacher = teacher;
            WeekType = weekType;
            IsOnline = isOnline;
        }
        
        
        
        public override string ToString()
        {
            return $"{string.Join(" ", GroupPriorities.ToList())}, {Teacher}";//, {}, {}, {}";
        }

        public RequisitionItem Copy()
        {
            return new(PlanItem, GroupPriorities, RepetitionsCount,
                MeetingTimePriorities, Teacher, WeekType);
        }
    }

    public record GroupRequisition(GroupsChoice[] GroupsChoices)
    {
        public override string ToString()
        {
            return String.Join("; ", GroupsChoices.ToList());
        }
    }

    public record MeetingTimeRequisition(MeetingTime[] MeetingTimeChoices);

    public sealed record GroupsChoice(MeetingGroup[] Groups)
    {
        public IEnumerable<MeetingGroup> GetGroupParts() => Groups.GetGroupParts();

        public override string ToString()
        {
            return String.Join(" ", Groups.ToList());
        }
    }
}