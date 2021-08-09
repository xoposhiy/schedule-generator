using System;
using System.Linq;

namespace Domain.ScheduleLib
{
    public class RequisitionItem
    {
        public LearningPlanItem PlanItem;
        public GroupRequisition[] GroupPriorities;

        //TODO: Нужно более строго смоделировать, что это либо комп-класс, либо класс с проектором, либо конкретный номер аудитории.
        public string Location;
        public int RepetitionsCount;

        public MeetingTimeRequisition[] MeetingTimePriorities;
        public Teacher Teacher;
        public WeekType WeekType;

        public RequisitionItem(LearningPlanItem planItem, GroupRequisition[] groupPriorities, string location, int repetitionsCount, MeetingTimeRequisition[] meetingTimePriorities, Teacher teacher, WeekType weekType)
        {
            PlanItem = planItem;
            GroupPriorities = groupPriorities;
            Location = location;
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
            return new(PlanItem, GroupPriorities, Location, RepetitionsCount,
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

    public record GroupsChoice(MeetingGroup[] Groups)
    {
        public override string ToString()
        {
            return String.Join(" ", Groups.ToList());
        }
    }
}