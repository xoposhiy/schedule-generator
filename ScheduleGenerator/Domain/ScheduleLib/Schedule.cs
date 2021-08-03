using System;

namespace Domain.ScheduleLib
{
    public record Schedule(Meeting[] Meetings);

    public record Discipline(string Name)
    {
        public override string ToString()
        {
            return Name;
        }
    }

    public class Meeting
    {
        public Discipline Discipline;
        public MeetingGroup[] Groups;
        public string? Location;
        public MeetingType MeetingType;
        public Teacher? Teacher;
        public MeetingTime? MeetingTime;
        public WeekType? WeekType;

        public Meeting(Discipline discipline, MeetingType meetingType, MeetingGroup[] groups)
        {
            Discipline = discipline;
            MeetingType = meetingType;
            Groups = groups;
        }

        public Meeting Copy()
        {
            var copy = new Meeting(Discipline, MeetingType, Groups)
            {
                Location = Location,
                Teacher = Teacher,
                MeetingTime = MeetingTime,
                WeekType = WeekType,
            };
            return copy;
        }

        public override string ToString()
        {
            return $"{Discipline}, {Groups}, {MeetingTime}, {WeekType}," +
                   $" Location: {Location}, MeetingType: {MeetingType}, Teacher: {Teacher}";
        }
    }

    public record MeetingGroup(string GroupName, GroupPart GroupPart)
    {
        public override string ToString()
        {
            return $"{GroupName} {GroupPart}";
        }
    }

    public enum MeetingType
    {
        Lecture,
        ComputerLab,
        Seminar,
        Online // TODO лишнее? должно быть понятно из Location
    }

    public record Teacher(string Name)
    {
        public override string ToString()
        {
            return Name;
        }
    }

    public record MeetingTime (DayOfWeek Day, int TimeSlotIndex)
    {
        public override string ToString()
        {
            return $"Day: {Day}, TimeSlotIndex: {TimeSlotIndex}";
        }
    }

    public enum WeekType
    {
        Any,
        Even,
        Odd
    }

    public enum GroupPart
    {
        FullGroup,
        Part1,
        Part2,
        Part3
    }
}