using CommonDomain.Enums;

namespace Domain2;

public record Room(Location Location, List<RoomSpec> RoomSpecs, HashSet<MeetingTime> Locked);