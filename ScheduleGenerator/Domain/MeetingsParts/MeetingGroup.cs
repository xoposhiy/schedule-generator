using System;
using Domain.Enums;

namespace Domain.MeetingsParts
{
    public record MeetingGroup(string GroupName, GroupPart GroupPart)
    {
        public override string ToString()
        {
            return $"{GroupName} {GroupPart}";
        }

        public string GetGroupSet()
        {
            var parts = GroupName.Split(new[] {"-"}, 2, StringSplitOptions.None);
            //ФИИТ-1
            //КН-2
            return $"{parts[0]}-{parts[1][0]}";
        }

        public string ToScheduleString()
        {
            return $"{GroupName}-{GroupPart.ToString()[^1]}";
        }
    }
}