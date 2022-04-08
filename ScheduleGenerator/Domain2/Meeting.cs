using CommonDomain;
using CommonDomain.Enums;
using Domain.Enums;

namespace Domain2;

public record Meeting2(
    Discipline Discipline,
    MeetingType MeetingType,
    Teacher Teacher,
    List<int> Groups,
    string Place,
    RoomSpec[] RoomSpecs,
    int Duration,
    WeekType WeekTypeSpec,
    List<MeetingTime> MeetingTimePriorities,
    MeetingType? After,
    bool HasEntranceTest,
    int Priority,
    bool IsFixed,
    bool ShouldBePlaced,

    string? ClassRoom,
    MeetingTime? MeetingTime
);