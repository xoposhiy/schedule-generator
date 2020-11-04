using System.Linq;

namespace ScheduleLib
{
    class Requesition
    {
        public string Discipline;
        public int DisciplineMeetingsCount;
        public GroupPriorityRequesition[] GroupPriorities;
        public string? Location;
        public int MeetingRepetitionsCount;
        public MeetingTime[] MeetingTimePriorities;
        public Teacher Teacher;
        public WeekType[] WeekTypes;

        public Requesition()
        {

        }
    }

    public class GroupPriorityRequesition
    {
        public GroupMask[] Groups;

        public GroupPriorityRequesition(GroupMask[] groups)
        {
            Groups = groups;
        }
    }

    public class GroupMask
    {
        public string[] Masks;

        public GroupMask(string priority)
        {
            Masks = priority.Split('+').Select(s => s.Trim()).ToArray();
        }
    }
}
