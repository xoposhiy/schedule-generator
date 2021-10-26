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

        public WeekType WeekType;
        public MeetingGroup[]? Groups;
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
                BaseMeeting = this,
                RequiredAdjacentMeeting = RequiredAdjacentMeeting
            };
        }

        public override string ToString()
        {
            var groupsString = Groups == null ? null : string.Join<MeetingGroup>(" ", Groups);
            return $"{Discipline}, Groups:[{groupsString}], Time:[{MeetingTime}, {WeekType}]," +
                   $"Location:[{Location}, {Classroom}], MeetingType: {MeetingType}, Teacher: {Teacher}";
        }
    }
}