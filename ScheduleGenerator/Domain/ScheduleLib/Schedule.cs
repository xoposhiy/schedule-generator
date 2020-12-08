﻿using System;

namespace Domain.ScheduleLib
{
    public class Schedule
    {
        public Meeting[] Meetings;

        public Schedule(Meeting[] meetings)
        {
            Meetings = meetings;
        }
    }

    public class Discipline
    {
        public string Name;

        public Discipline(string name)
        {
            Name = name;
        }

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

        public override string ToString()
        {
            return $"{Discipline}, {Groups}, {MeetingTime}, {WeekType}, Location: {Location}, MeetingType: {MeetingType}, Teacher: {Teacher}";
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

        public override string ToString()
        {
            return $"{GroupName} {GroupPart}";
        }
    }

    public enum MeetingType
    {
        Lecture,
        ComputerLab,
        Seminar
    }

    public class Teacher
    {
        public string Name;

        public Teacher(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class MeetingTime
    {
        public DayOfWeek Day;
        public int TimeSlotIndex;

        public MeetingTime(DayOfWeek day, int timeSlotIndex)
        {
            Day = day;
            TimeSlotIndex = timeSlotIndex;
        }

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