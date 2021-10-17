using System;
using System.Linq;

namespace Domain
{
    public class Meeting
    {
        public const string OnlineLocationName = "Онлайн";
        public readonly RequisitionItem RequisitionItem;
        public readonly Discipline Discipline;
        public readonly MeetingType MeetingType;
        public Teacher Teacher;
        public WeekType WeekType;
        public MeetingGroup[]? Groups;
        public string? Location;
        public MeetingTime? MeetingTime;
        public Meeting? BaseMeeting;
        public Meeting? RequiredAdjacentMeeting;

        public Meeting(Discipline discipline, MeetingType meetingType, Teacher teacher, WeekType weekType,
            RequisitionItem requisitionItem, MeetingGroup[]? groups = null)
        {
            Discipline = discipline;
            MeetingType = meetingType;
            Groups = groups;
            Teacher = teacher;
            WeekType = weekType;
            RequisitionItem = requisitionItem;
        }

        public Meeting BasicCopy()
        {
            return new(
                Discipline,
                MeetingType,
                Teacher,
                WeekType,
                RequisitionItem,
                Groups)
            {
                BaseMeeting = this,
                RequiredAdjacentMeeting = RequiredAdjacentMeeting
            };
        }

        public override string ToString()
        {
            var groupsString = Groups == null ? null : string.Join<MeetingGroup>(" ", Groups);
            return $"{Discipline}, Groups: [{groupsString}], Time: {MeetingTime}, {WeekType}," +
                   $" Location: {Location}, MeetingType: {MeetingType}, Teacher: {Teacher}";
        }

        public bool GroupsEquals(MeetingGroup[] meetingGroups)
        {
            var f = Groups!.ToHashSet();
            var g = meetingGroups.ToHashSet();
            return f.SetEquals(g);
        }
    }

    public record MeetingGroup(string GroupName, GroupPart GroupPart)
    {
        public override string ToString()
        {
            return $"{GroupName} {GroupPart}";
        }

        public string GetGroupSet()
        {
            var parts = GroupName.Split(new[] {"-"}, 2, StringSplitOptions.None);
            //ФИИТ-1
            //КН-2
            return $"{parts[0]}-{parts[1][0]}";
        }
    }


    public enum MeetingType
    {
        Lecture,
        ComputerLab,
        Seminar
    }


    public record Discipline(string Name)
    {
        public override string ToString()
        {
            return Name;
        }
    }

    public record Teacher(string Name)
    {
        public override string ToString()
        {
            return Name;
        }
    }

    public record MeetingTime(DayOfWeek Day, int TimeSlotIndex)
    {
        public override string ToString()
        {
            return $"Day: {Day}, TimeSlotIndex: {TimeSlotIndex}";
        }
    }

    public enum WeekType
    {
        All,
        Even,
        Odd,
        OddOrEven
    }

    public enum GroupPart
    {
        FullGroup,
        Part1,
        Part2
    }
}