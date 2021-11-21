using System;
using Domain.Enums;
using Domain.MeetingsParts;

namespace Domain
{
    public class Meeting
    {
        public const string English = "ИнЯз";
        public readonly RequisitionItem RequisitionItem;
        public Location Location => RequisitionItem.Location;
        public Teacher Teacher => RequisitionItem.Teacher;
        public LearningPlanItem PlanItem => RequisitionItem.PlanItem;
        public Discipline Discipline => PlanItem.Discipline;
        public MeetingType MeetingType => PlanItem.MeetingType;
        public int Priority => PlanItem.Priority;
        public bool IsRoomNeeded => Location == Location.MathMeh && Discipline.Name != English;
        public double Weight => WeekType == WeekType.All ? 1 : 0.5;

        public WeekType WeekType;
        public GroupsChoice? GroupsChoice;
        public string? Classroom;
        public MeetingTime? MeetingTime;

        public Meeting? BaseMeeting;
        public Meeting? RequiredAdjacentMeeting;

        public Meeting(WeekType weekType, RequisitionItem requisitionItem)
        {
            WeekType = weekType;
            RequisitionItem = requisitionItem;
        }

        public Meeting BasicCopy()
        {
            return new(WeekType, RequisitionItem)
            {
                BaseMeeting = this
            };
        }
        
        private string? stringValue;

        public override string ToString()
        {
            if (stringValue != null)
                return stringValue;
            var groupsString = GroupsChoice?.ToString() ??
                               string.Join<GroupRequisition>("|", RequisitionItem.GroupPriorities);
            
            return stringValue = $"{Discipline}, Groups:[{groupsString}], Time:[{MeetingTime}, {WeekType.GetPrettyString()}]," +
                   $"Location:[{Location}, {Classroom}], MeetingType: {MeetingType}, Teacher: {Teacher}";
        }
    }
}