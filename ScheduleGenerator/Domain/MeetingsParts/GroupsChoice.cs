using System.Collections.Generic;
using System.Linq;

namespace Domain.MeetingsParts
{
    public sealed record GroupsChoice(MeetingGroup[] Groups)
    {
        public IEnumerable<MeetingGroup> GetGroupParts()
        {
            return Groups.GetGroupParts();
        }

        public override string ToString()
        {
            return string.Join(" ", Groups.ToList());
        }
    }
}