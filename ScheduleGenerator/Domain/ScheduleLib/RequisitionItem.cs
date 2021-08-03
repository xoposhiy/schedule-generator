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
            return $"{PlanItem}, {Teacher}";//, {}, {}, {}";
        }
    }

    public class GroupRequisition
    {
        public GroupsChoice[] GroupsChoices;

        public GroupRequisition(GroupsChoice[] groupsChoices)
        {
            GroupsChoices = groupsChoices;
        }
    }

    public class MeetingTimeRequisition
    {
        public MeetingTime[] MeetingTimeChoices;

        public MeetingTimeRequisition(MeetingTime[] meetingTimeChoices)
        {
            MeetingTimeChoices = meetingTimeChoices;
        }
    }

    public class GroupsChoice
    {
        public MeetingGroup[] Groups;

        public GroupsChoice(MeetingGroup[] groups)
        {
            Groups = groups;
        }
    }
}