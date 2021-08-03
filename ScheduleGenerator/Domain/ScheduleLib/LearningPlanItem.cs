namespace Domain.ScheduleLib
{
    public record LearningPlanItem(string Group, Discipline Discipline,
        MeetingType MeetingType, GroupSize GroupSize, double MeetingsPerWeek)
    {
        public override string ToString()
        {
            return $"{Group}, {Discipline}, {MeetingType}, {GroupSize}, {MeetingsPerWeek}";
        }
    }
}