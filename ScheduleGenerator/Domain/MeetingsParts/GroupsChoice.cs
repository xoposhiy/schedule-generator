using System.Collections.Generic;
using System.Linq;

namespace Domain.MeetingsParts
{
    public sealed record GroupsChoice(MeetingGroup[] Groups)
    {
        public HashSet<MeetingGroup> GetGroupParts()
        {
            return Groups.GetGroupParts();
        }

        public override string ToString()
        {
            var groupStrings = Groups.Select(g => g.ToString()).OrderBy(s => s);
            return string.Join(" ", groupStrings);
        }
    }
}