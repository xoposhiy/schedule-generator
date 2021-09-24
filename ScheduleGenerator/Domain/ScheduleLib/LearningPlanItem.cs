namespace Domain.ScheduleLib
{
    public record LearningPlanItem(string GroupSet, Discipline Discipline,
        MeetingType MeetingType, GroupSize GroupSize, double MeetingsPerWeek, RoomSpec[] RoomSpecs,
        MeetingType? RequiredAdjacentMeetingType, MeetingType? SameTeacherWith)
    {
        public override string ToString()
        {
            return $"{GroupSet}, {Discipline}, {MeetingType}, {GroupSize}, {MeetingsPerWeek}";
        }
    }

    public enum RoomSpec
    {
        Big,
        ForGroup,
        Computer,
        Projector
    }
}