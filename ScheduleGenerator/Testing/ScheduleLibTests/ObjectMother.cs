using System.Collections.Generic;
using Domain.ScheduleLib;

namespace Testing.ScheduleLibTests
{
    public static class ObjectMother
    {
        public static Dictionary<string, List<RoomSpec>> ClassRooms = new()
        {
            ["632"] = new List<RoomSpec> {RoomSpec.Big, RoomSpec.Projector},
            ["605"] = new List<RoomSpec> {RoomSpec.ForGroup},
        };


        public static MeetingGroup Group1 = new MeetingGroup("ФИИТ-101", GroupPart.FullGroup);
        public static MeetingGroup Group1_1 = new MeetingGroup("ФИИТ-101-1", GroupPart.Part1);
        public static MeetingGroup Group1_2 = new MeetingGroup("ФИИТ-101-2", GroupPart.Part2);

        public static Discipline OOP = new Discipline("ООП");
        public static Discipline Calculus = new Discipline("МатАн");
        public static Discipline Unix = new Discipline("Unix");

        public static LearningPlanItem CalculusLecture = new LearningPlanItem("ФИИТ-1", Calculus, MeetingType.Lecture,
            GroupSize.FullGroup, 1.5, new[] {RoomSpec.Big}, null, null);

        public static LearningPlanItem OopSeminar = new LearningPlanItem("ФИИТ-101-1", OOP, MeetingType.Seminar,
            GroupSize.HalfGroup, 1, System.Array.Empty<RoomSpec>(), MeetingType.ComputerLab, null);

        public static LearningPlanItem OopLab = new LearningPlanItem("ФИИТ-101-1", OOP, MeetingType.ComputerLab,
            GroupSize.HalfGroup, 1, new[]
            {
                RoomSpec.Computer,
            }, null, null);

        public static Teacher OopTeacher = new Teacher("Петряшов");
    }
}