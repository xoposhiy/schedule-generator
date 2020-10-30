using System;

namespace ScheduleLib
{
    public class Schedule
    {
        public Meeting[] Meetings;

        public Schedule(Meeting[] meetings)
        {
            Meetings = meetings;
        }
    }

    public class Meeting
    {
        public DayOfWeek? DayOfWeek;

        public string Discipline;
        public MeetingGroup[] Groups;
        public string? Location;
        public MeetingType MeetingType;
        public Teacher? Teacher;
        public int? TimeSlotIndex;
        public WeekType? WeekType;

        public Meeting(string discipline, MeetingType meetingType, MeetingGroup[] groups)
        {
            Discipline = discipline;
            MeetingType = meetingType;
            Groups = groups;
        }
    }

    public class MeetingGroup
    {
        public string GroupName;
        public GroupPart GroupPart;

        public MeetingGroup(string groupName, GroupPart groupPart)
        {
            GroupName = groupName;
            GroupPart = groupPart;
        }
    }

    public enum MeetingType
    {
        Lecture,
        Lab,
        Seminar
    }

    public class Teacher
    {
        public string Name;

        public Teacher(string name)
        {
            Name = name;
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