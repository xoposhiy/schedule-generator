using CommonDomain;
using CommonDomain.Enums;
using Domain.Enums;

namespace Domain2;

/// <summary>
/// 
/// </summary>
/// <param name="Discipline"></param>
/// <param name="MeetingType"></param>
/// <param name="Teacher"></param>
/// <param name="Groups"></param>
/// <param name="Place"></param>
/// <param name="RoomSpecs"></param>
/// <param name="Duration"></param>
/// <param name="WeekTypeSpec">Требование четности для пары (в большенсвте случаев оно будет либо All, либо OddOrEven)</param>
/// <param name="MeetingTimePriorities">Удобство для преподователя, может содержать чётность</param>
/// <param name="After"></param>
/// <param name="HasEntranceTest"></param>
/// <param name="Priority"></param>
/// <param name="IsFixed"></param>
/// <param name="Ignore"></param>
/// <param name="ClassRoom"></param>
/// <param name="MeetingTime"></param>
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
    bool Ignore,

    string? ClassRoom,
    MeetingTime? MeetingTime
);