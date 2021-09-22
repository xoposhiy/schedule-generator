using System;
using System.Linq;

namespace Domain.ScheduleLib
{
    public class Meeting
    {
        public RequisitionItem RequisitionItem;
        public Discipline Discipline;
        public MeetingType MeetingType;
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
            return new Meeting(
                Discipline,
                MeetingType, 
                Teacher,
                WeekType,
                RequisitionItem)
            {
                BaseMeeting = this,
                RequiredAdjacentMeeting = this.RequiredAdjacentMeeting
            };
            
        }

        public override string ToString()
        {
            return $"{Discipline}, {Groups}, {MeetingTime}, {WeekType}," +
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
        Seminar,
        Online 
        // TODO Online нужно указывать у RequisitionItem, а не у LearningPlanItem: в онлайн могут уходить отдельные преподаватели по курсу, а не все сразу. Нужно перенести туда.  
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

    public record MeetingTime (DayOfWeek Day, int TimeSlotIndex)
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
        Part2,
    }
}