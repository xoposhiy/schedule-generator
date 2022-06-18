using CommonDomain.Enums;

namespace Domain2;

/// <summary>
/// Описание аудитории
/// </summary>
/// <param name="Location">Место МатМех, Онлайн и т.д</param>
/// <param name="RoomSpecs">Требования, которые выполняет комната</param>
/// <param name="Locked">Время, когда аудитория недоступна</param>
public record Room(Location Location, List<RoomSpec> RoomSpecs, HashSet<MeetingTime> Locked);