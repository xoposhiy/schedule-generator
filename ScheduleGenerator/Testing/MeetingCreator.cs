using System;
using System.Collections.Generic;
using System.Linq;

using Domain.ScheduleLib;


namespace Testing
{
    public class MeetingCreator
    {
        public static LearningPlanItem StandardLearningPlanItem = new LearningPlanItem(
            "FT-202", new Discipline("OOP"), MeetingType.Lecture, GroupSize.HalfGroup, 2);

        public static Requisition StandardRequisition = new Requisition(new RequisitionItem[]
        {
            new RequisitionItem(
                StandardLearningPlanItem,
                new[]
                {
                    new GroupRequisition(new[] {new GroupsChoice(new[] {new MeetingGroup("FT-202", GroupPart.Part1)})})
                },
                "514",
                2,
                new[] {new MeetingTimeRequisition(new[] {new MeetingTime(DayOfWeek.Tuesday, 2)})},
                new Teacher("Eg"),
                WeekType.Any)
        });
        public static Schedule StandardSchedule = new Schedule(new[] {
                MeetingCreator.CreateMeeting("Math 623 Fil 0 3 0 0 FT-202#0"),
                MeetingCreator.CreateMeeting("DM 622 Str 0 0 0 0 FT-201#0"),
                //MeetingCreator.CreateMeeting("OOP 514 Eg 0 2 0 0 FT-202#1")
            });
        public static LearningPlan StandardLearningPlan = new LearningPlan(
            new[]{
                new LearningPlanItem("FT-202", new Discipline("OOP"), MeetingType.Lecture, GroupSize.HalfGroup, 2),
                new LearningPlanItem("FT-202", new Discipline("OOP"), MeetingType.Seminar, GroupSize.HalfGroup, 2)
            });

        // Math 623 Fil 0 3 0 0 FT-202#0 FT-201#0 KN-201#2
        public static Meeting CreateMeeting(string entry)
        {
            var parts = entry.Split();
            var discipline = parts[0];
            var location = parts[1];
            var teacher = new Teacher(parts[2]);
            var dayOfWeek = (DayOfWeek)((int.Parse(parts[3]) + 1) % 7);
            var slotIndex = int.Parse(parts[4]);
            var weekType = (WeekType)int.Parse(parts[5]);
            var meetingType = (MeetingType)int.Parse(parts[6]);
            var groups = new List<MeetingGroup>();
            foreach (var e in parts.Skip(7))
            {
                var namePart = e.Split('#');
                groups.Add(new MeetingGroup(namePart[0], (GroupPart)int.Parse(namePart[1])));
            }
            var meeting = new Meeting(new Discipline(discipline), meetingType, groups.ToArray());
            meeting.Location = location;
            meeting.Teacher = teacher;
            meeting.MeetingTime = new MeetingTime(dayOfWeek, slotIndex);
            meeting.WeekType = weekType;
            return meeting;
        }
    }
}
