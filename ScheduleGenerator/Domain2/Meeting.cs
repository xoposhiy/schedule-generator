using CommonDomain;
using CommonDomain.Enums;

namespace Domain2;

public record Meeting
{
    public readonly MeetingType? After;
    public readonly int AmountPerWeek;
    public readonly string? Classroom;
    public readonly Discipline Discipline;
    public readonly int Duration;
    public readonly List<int> Groups;
    public readonly bool HasEntranceTest;
    public readonly Location Location;
    public readonly List<MeetingTime> MeetingTimePriorities;
    public readonly RoomSpec[] RoomSpecs;
    public readonly Teacher Teacher;
    public bool ShouldBePlaced => AmountPerWeek != 0;
}