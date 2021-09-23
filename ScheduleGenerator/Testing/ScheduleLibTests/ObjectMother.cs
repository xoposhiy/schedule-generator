using System.Collections.Generic;
using Domain.ScheduleLib;

namespace Testing.ScheduleLibTests
{
    public static class ObjectMother
    {
        public static readonly Dictionary<string, List<RoomSpec>> ClassRooms = new()
        {
            ["632"] = new List<RoomSpec> {RoomSpec.Big, RoomSpec.Projector},
            ["605"] = new List<RoomSpec> {RoomSpec.ForGroup},
            ["514"] = new List<RoomSpec> {RoomSpec.Computer, RoomSpec.ForGroup},
        };


        public static readonly MeetingGroup Group1 = new MeetingGroup("ФИИТ-101", GroupPart.FullGroup);
        public static readonly MeetingGroup Group1_1 = new MeetingGroup("ФИИТ-101-1", GroupPart.Part1);
        public static readonly MeetingGroup Group1_2 = new MeetingGroup("ФИИТ-101-2", GroupPart.Part2);
        public static readonly MeetingGroup Group2 = new MeetingGroup("ФИИТ-102", GroupPart.FullGroup);

        public static readonly Discipline OOP = new Discipline("ООП");
        public static readonly Discipline Calculus = new Discipline("МатАн");
        public static readonly Discipline Unix = new Discipline("Unix");

        public static readonly LearningPlanItem CalculusLecture = new LearningPlanItem("ФИИТ-1", Calculus, MeetingType.Lecture,
            GroupSize.FullGroup, 1.5, new[] {RoomSpec.Big}, null, null);
        
        public static readonly LearningPlanItem CalculusSeminar = new LearningPlanItem("ФИИТ-1", Calculus, MeetingType.Seminar,
            GroupSize.FullGroup, 1, new[] {RoomSpec.Big}, null, null);

        public static readonly LearningPlanItem OopSeminar = new LearningPlanItem("ФИИТ-101-1", OOP, MeetingType.Seminar,
            GroupSize.HalfGroup, 1, System.Array.Empty<RoomSpec>(), MeetingType.ComputerLab, null);

        public static readonly LearningPlanItem OopLab = new LearningPlanItem("ФИИТ-101-1", OOP, MeetingType.ComputerLab,
            GroupSize.HalfGroup, 1, new[]
            {
                RoomSpec.Computer,
            }, null, MeetingType.Seminar);

        public static readonly Teacher OopTeacher1 = new Teacher("Петряшов");
        public static readonly Teacher OopTeacher2 = new Teacher("Романюк");
        public static readonly Teacher CalculusTeacher = new Teacher("Филатова");
    }
}