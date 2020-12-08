namespace Domain.ScheduleLib
{
    public class LearningPlanItem
    {
        public LearningPlanItem(string group, Discipline discipline, MeetingType meetingType, GroupSize groupSize, double meetingsPerWeek)
        {
            Group = group;
            Discipline = discipline;
            MeetingType = meetingType;
            GroupSize = groupSize;
            MeetingsPerWeek = meetingsPerWeek;
        }

        public string Group;
        public Discipline Discipline;
        public MeetingType MeetingType;
        public GroupSize GroupSize;
        public double MeetingsPerWeek;
    }
}