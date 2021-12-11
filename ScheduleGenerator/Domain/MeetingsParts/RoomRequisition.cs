using System.Collections.Generic;
using Domain.Enums;

namespace Domain.MeetingsParts
{
    public record RoomRequisition(string Room, List<RoomSpec> RoomSpecs, HashSet<MeetingTime> LockedTimes);
}