using System.Globalization;
using Domain.Enums;

namespace Domain.MeetingsParts
{
    public record LearningPlanItem(string GroupSet, Discipline Discipline,
        MeetingType MeetingType, GroupSize GroupSize, double MeetingsPerWeek, RoomSpec[] RoomSpecs,
        MeetingType? RequiredAdjacentMeetingType, MeetingType? SameTeacherWith, int Priority = 0)
    {
        public override string ToString()
        {
            var perWeek = MeetingsPerWeek.ToString(CultureInfo.InvariantCulture);
            return $"{GroupSet}, {Discipline}, {MeetingType}, {GroupSize}, {perWeek}, {Priority}";
        }
    }
}