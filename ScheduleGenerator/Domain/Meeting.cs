using Domain.Enums;
using Domain.MeetingsParts;

namespace Domain
{
    public class Meeting
    {
        public const string English = "ИнЯз";
        public readonly RequisitionItem RequisitionItem;

        private int hashcode;
        public Meeting? RequiredAdjacentMeeting;

        private string? stringValue;

        public Meeting(WeekType weekType, RequisitionItem requisitionItem)
        {
            WeekType = weekType;
            RequisitionItem = requisitionItem;
        }

        public Location Location => RequisitionItem.Location;
        public Teacher Teacher => RequisitionItem.Teacher;
        public LearningPlanItem PlanItem => RequisitionItem.PlanItem;
        public Discipline Discipline => PlanItem.Discipline;
        public MeetingType MeetingType => PlanItem.MeetingType;
        public int Priority => PlanItem.Priority;
        public bool IsRoomNeeded => Location == Location.MathMeh && Discipline.Name != English;
        public double Weight => WeekType == WeekType.All ? 1 : 0.5;
        public WeekType WeekType { get; private init; }
        public GroupsChoice? GroupsChoice { get; private init; }
        public string? Classroom { get; private init; }
        public MeetingTime? MeetingTime { get; private init; }

        public Meeting? BaseMeeting { get; private init; }

        public Meeting BasicCopy(GroupsChoice groupsChoice, MeetingTime meetingTime, string? room, WeekType weekType)
        {
            return new(WeekType, RequisitionItem)
            {
                BaseMeeting = this,
                GroupsChoice = groupsChoice,
                MeetingTime = meetingTime,
                Classroom = room,
                WeekType = weekType
            };
        }

        public override string ToString()
        {
            if (stringValue != null)
                return stringValue;
            var groupsString = GroupsChoice?.ToString() ??
                               string.Join<GroupRequisition>("|", RequisitionItem.GroupPriorities);

            return stringValue =
                $"{Discipline}, Groups:[{groupsString}], Time:[{MeetingTime}, {WeekType.GetPrettyString()}]," +
                $"Location:[{Location}, {Classroom}], MeetingType: {MeetingType}, Teacher: {Teacher}";
        }

        public override int GetHashCode()
        {
            if (hashcode != 0) return hashcode;
            return hashcode = ToString().GetHashCode();
        }
    }
}