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
        public MeetingType MeetingType;
        public Teacher Teacher;
        public WeekType[]? WeekTypes;

        public Requesition(string discipline,
                           int disciplineMeetingsCount,
                           int meetingRepetitionsCount,
                           MeetingTime[] meetingTimePriorities,
                           MeetingType meetingType,
                           Teacher teacher
                           )
        {
            Discipline = discipline;
            DisciplineMeetingsCount = disciplineMeetingsCount;
            MeetingRepetitionsCount = meetingRepetitionsCount;
            MeetingTimePriorities = meetingTimePriorities;
            MeetingType = meetingType;
            Teacher = teacher;
        }
    }

    public class GroupPriorityRequesition
    {
        public string[] Masks;

        public GroupPriorityRequesition(string priority)
        {
            Masks = priority.Split('+').Select(s => s.Trim()).ToArray();
        }
    }
}
