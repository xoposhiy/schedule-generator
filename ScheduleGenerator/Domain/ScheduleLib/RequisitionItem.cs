using System;
using System.Linq;

namespace Domain.ScheduleLib
{
    public class RequisitionItem
    {
        public readonly LearningPlanItem PlanItem;
        public readonly GroupRequisition[] GroupPriorities;
        
        public readonly int RepetitionsCount;

        public readonly MeetingTimeRequisition[] MeetingTimePriorities;
        public readonly Teacher Teacher;
        public readonly WeekType WeekType;

        public RequisitionItem(LearningPlanItem planItem, GroupRequisition[] groupPriorities, int repetitionsCount,
            MeetingTimeRequisition[] meetingTimePriorities, Teacher teacher, WeekType weekType)
        {
            PlanItem = planItem;
            GroupPriorities = groupPriorities;
            RepetitionsCount = repetitionsCount;
            MeetingTimePriorities = meetingTimePriorities;
            Teacher = teacher;
            WeekType = weekType;
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
        public override string ToString()
        {
            return String.Join(" ", Groups.ToList());
        }
    }
}