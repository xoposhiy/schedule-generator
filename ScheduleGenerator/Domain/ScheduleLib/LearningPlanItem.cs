namespace Domain.ScheduleLib
{
    //TODO чтение из таблицы
    public record LearningPlanItem(string GroupSet, Discipline Discipline,
        MeetingType MeetingType, GroupSize GroupSize, double MeetingsPerWeek, RoomSpec[] RoomSpecs,
        MeetingType? ConnectAfter, MeetingType? SameTeacherWith)
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
        Projector,
        Any
        //TODO
    }
}