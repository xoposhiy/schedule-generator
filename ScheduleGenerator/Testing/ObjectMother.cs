using System.Collections.Generic;
using Domain.Enums;
using Domain.MeetingsParts;
using static Domain.DomainExtensions;
using static Infrastructure.SheetConstants;

namespace Testing
{
    public static class ObjectMother
    {
        public static readonly Requisition AutumnRequisition;
        public static readonly List<RoomRequisition> ClassroomsRequisitions;

        static ObjectMother()
        {
            (AutumnRequisition, ClassroomsRequisitions) =
                GetRequisition(AutumnConfig, Repository);
        }

        public static readonly MeetingGroup Group1 = new("ФИИТ-101", GroupPart.FullGroup);
        public static readonly MeetingGroup Group11 = new("ФИИТ-101-1", GroupPart.Part1);
        public static readonly MeetingGroup Group12 = new("ФИИТ-101-2", GroupPart.Part2);
        public static readonly MeetingGroup Group2 = new("ФИИТ-102", GroupPart.FullGroup);

        public static readonly Discipline Oop = new("ООП");
        public static readonly Discipline Calculus = new("МатАн");
        public static readonly Discipline Os = new("ОС");
        public static readonly Discipline Pe = new("Физкультура");

        public static readonly LearningPlanItem OsLecture = new("ФИИТ-1", Os, MeetingType.Lecture,
            GroupSize.FullGroup, 1, new[] {RoomSpec.Big}, null, null);

        public static readonly LearningPlanItem CalculusLecture = new("ФИИТ-1", Calculus,
            MeetingType.Lecture,
            GroupSize.FullGroup, 1.5, new[] {RoomSpec.Big}, null, null);

        public static readonly LearningPlanItem CalculusSeminar = new("ФИИТ-1", Calculus,
            MeetingType.Seminar,
            GroupSize.FullGroup, 1, new[] {RoomSpec.Big}, null, null);

        public static readonly LearningPlanItem OopSeminar = new("ФИИТ-101-1", Oop,
            MeetingType.Seminar,
            GroupSize.HalfGroup, 1, System.Array.Empty<RoomSpec>(), MeetingType.ComputerLab, null);

        public static readonly LearningPlanItem OopLab = new("ФИИТ-101-1", Oop,
            MeetingType.ComputerLab,
            GroupSize.HalfGroup, 1, new[]
            {
                RoomSpec.Computer
            }, null, MeetingType.Seminar);

        public static readonly LearningPlanItem PeSeminar = new("ФИИТ-202 + ФИИТ-201", Pe, MeetingType.Seminar,
            GroupSize.FullGroup, 2, System.Array.Empty<RoomSpec>(), null, null, 1);

        public static readonly Teacher OopTeacher1 = new("Петряшов");
        public static readonly Teacher OopTeacher2 = new("Романюк");
        public static readonly Teacher CalculusTeacher = new("Филатова");
        public static readonly Teacher OsTeacher = new("Копейцев");
        public static readonly Teacher PeTeacher = new("Физрук");

        public static readonly RequisitionItem OopLabRequisitionItem =
            new(OopLab, "ФИИТ-101", "пн: 5-6 пара", OopTeacher2);

        public static readonly Requisition CalculusRequisition = new(new[]
        {
            new RequisitionItem(CalculusLecture, "ФИИТ-101", "пн: 1-6 пара", CalculusTeacher,
                1, Location.Online),
            new RequisitionItem(CalculusSeminar, "ФИИТ-101", "пн: 1-6 пара", CalculusTeacher)
        });

        public static readonly Requisition OsRequisition = new(new[]
        {
            new RequisitionItem(OsLecture, "ФИИТ-101+ФИИТ-102", "вт: 3 пара", OsTeacher)
        });

        public static readonly Requisition FullMondayRequisition = new(new[]
        {
            new RequisitionItem(CalculusLecture, "ФИИТ-101", "пн: 1-6 пара", CalculusTeacher),
            new RequisitionItem(CalculusSeminar, "ФИИТ-101", "пн: 3-6 пара", CalculusTeacher),
            new RequisitionItem(OsLecture, "ФИИТ-101", "пн: 4-6 пара", OsTeacher),
            new RequisitionItem(OopLab, "ФИИТ-101", "пн: 5-6 пара", OopTeacher2),
            new RequisitionItem(OopSeminar, "ФИИТ-101", "пн: 6 пара", OopTeacher2)
        });

        public static readonly Requisition NecessaryMeetings = new(new[]
        {
            new RequisitionItem(PeSeminar, "ФИИТ-202 + ФИИТ-201", "ср, пт: 1 пара", PeTeacher)
        });
    }
}