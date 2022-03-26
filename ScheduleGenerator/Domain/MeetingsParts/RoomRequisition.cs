using System.Collections.Generic;
using CommonDomain.Enums;

namespace Domain.MeetingsParts
{
    public record RoomRequisition(string Room, List<RoomSpec> RoomSpecs, HashSet<MeetingTime> LockedTimes);
}