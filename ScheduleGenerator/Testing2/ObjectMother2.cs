using CommonDomain;
using CommonDomain.Enums;
using Domain2;

namespace Testing2;

public static class ObjectMother2
{
    public static readonly Discipline FirstDiscipline = new("Discipline1");
    public static readonly Teacher FirstTeacher = new("Teacher");
    public static readonly List<int> FirstGroup = new() {1};
    public static readonly MeetingTime FirstMeetingTime = new(WeekType.All, DayOfWeek.Monday, 2);
    public static readonly MeetingTime SecondMeetingTime = FirstMeetingTime with {DayOfWeek = DayOfWeek.Wednesday};

    public static readonly Meeting2 FirstMeeting =
        new(0, FirstDiscipline, MeetingType.Lecture, FirstTeacher, FirstGroup, "Place1",
            Array.Empty<RoomSpec>(), 1, WeekType.All, new(), MeetingType.Lecture, 2, false, false, null,
            FirstMeetingTime);


    public static State CreateEmptyState(params Meeting2[] meetings)
    {
        return new(meetings, new());
    }
}