using CommonDomain;
using CommonDomain.Enums;

namespace Domain2;

public class Meeting
{
    public readonly MeetingType? After;
    public readonly int AmountPerWeek;
    public readonly string? Classroom;
    public readonly Discipline Discipline;
    public readonly int Duration;
    public readonly int GroupIndex;
    public readonly bool HasTests;
    public readonly Location Location;
    public readonly List<MeetingTime> MeetingTimePriorities;
    public readonly RoomSpec[] RoomSpecs;
    public readonly Teacher Teacher;
    public readonly WeekType WeekType;
    public bool ShouldBePlaced => AmountPerWeek != 0;
}