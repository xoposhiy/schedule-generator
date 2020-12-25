namespace Domain.ScheduleLib
{
    public class Requisition
    {
        public LearningPlanItem PlanItem;
        public GroupRequisition[] GroupPriorities;

        //TODO: Нужно более строго смоделировать, что это либо комп-класс, либо класс с проектором, либо конкретный номер аудитории.
        public string Location;
        public int RepetitionsCount;
        
        public MeetingTimeRequesition[] MeetingTimePriorities;
        public Teacher Teacher;
        public WeekType WeekType;

        public Requisition(LearningPlanItem planItem, GroupRequisition[] groupPriorities, string location, int repetitionsCount, MeetingTimeRequesition[] meetingTimePriorities, Teacher teacher, WeekType weekType)
        {
            PlanItem = planItem;
            GroupPriorities = groupPriorities;
            Location = location;
            RepetitionsCount = repetitionsCount;
            MeetingTimePriorities = meetingTimePriorities;
            Teacher = teacher;
            WeekType = weekType;
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

    public class MeetingTimeRequesition
    {
        public MeetingTime[] MeetingTimeChoices;

        public MeetingTimeRequesition(MeetingTime[] meetingTimeChoices)
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
