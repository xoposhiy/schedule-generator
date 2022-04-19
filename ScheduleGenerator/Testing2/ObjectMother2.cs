using CommonDomain;
using CommonDomain.Enums;
using Domain2;

namespace Testing2;

public static class ObjectMother2
{
    public static Discipline FirstDiscipline = new("Discipline1");
    public static Teacher FirstTeacher = new("Teacher");
    public static List<int> FirstGroup = new() {1};

    public static Meeting2 FirstMeeting =
        new(0, FirstDiscipline, MeetingType.Lecture, FirstTeacher, FirstGroup, "Place1",
            Array.Empty<RoomSpec>(), 1, WeekType.All, new(), MeetingType.Lecture, 2, false, false, null, null);
}