using System;
using System.Collections.Generic;
using System.Linq;
using Domain.ScheduleLib;


namespace Testing
{
    public class MeetingCreator
    {
        public static LearningPlanItem StandardLearningPlanItem = new(
            "FT-202", new("OOP"), MeetingType.Lecture, GroupSize.HalfGroup, 2,
            Array.Empty<RoomSpec>(), null, null);

        public static Requisition StandardRequisition = new(new[]
        {
            new RequisitionItem(
                StandardLearningPlanItem,
                new[]
                {
                    new GroupRequisition(new[] {new GroupsChoice(new[] {new MeetingGroup("FT-202", GroupPart.Part1)})})
                },
                2,
                new[] {new MeetingTimeRequisition(new[] {new MeetingTime(DayOfWeek.Tuesday, 2)})},
                new("Eg"))
        });

        // Math 623 Fil 0 3 0 0 FT-202#0 FT-201#0 KN-201#2
        public static Meeting CreateMeeting(string entry)
        {
            var parts = entry.Split();
            var discipline = parts[0];
            var location = parts[1];
            var teacher = new Teacher(parts[2]);
            var dayOfWeek = (DayOfWeek) ((int.Parse(parts[3]) + 1) % 7);
            var slotIndex = int.Parse(parts[4]);
            var weekType = (WeekType) int.Parse(parts[5]);
            var meetingType = (MeetingType) int.Parse(parts[6]);
            var groups = new List<MeetingGroup>();
            foreach (var e in parts.Skip(7))
            {
                var namePart = e.Split('#');
                groups.Add(new(namePart[0], (GroupPart) int.Parse(namePart[1])));
            }

            var meeting = new Meeting(new(discipline), meetingType, teacher, weekType,
                StandardRequisition.Items[0], groups.ToArray());
            meeting.Location = location;
            meeting.Teacher = teacher;
            meeting.MeetingTime = new(dayOfWeek, slotIndex);
            meeting.WeekType = weekType;
            return meeting;
        }
    }
}