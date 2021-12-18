using System.Collections.Generic;
using System.Globalization;
using Domain.Enums;

namespace Domain.MeetingsParts
{
    public record LearningPlanItem(string GroupSet, Discipline Discipline,
        MeetingType MeetingType, GroupSize GroupSize, double MeetingsPerWeek, RoomSpec[] RoomSpecs,
        HashSet<Discipline> UnwantedDisciplines,
        MeetingType? RequiredAdjacentMeetingType, MeetingType? SameTeacherWith, int Priority = 0, bool IsHard = false)
    {
        public override string ToString()
        {
            var perWeek = MeetingsPerWeek.ToString(CultureInfo.InvariantCulture);
            return $"{GroupSet}, {Discipline}, {MeetingType}, {GroupSize}, {perWeek}, {Priority}, IsHard: {IsHard}";
        }
    }
}