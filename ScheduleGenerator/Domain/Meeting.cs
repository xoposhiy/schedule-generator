using System;
using CommonDomain;
using CommonDomain.Enums;
using Domain.Enums;
using Domain.MeetingsParts;

namespace Domain
{
    public class Meeting
    {
        public const string English = "ИнЯз";
        public readonly RequisitionItem RequisitionItem;

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

        private bool Equals(Meeting other)
        {
            if (BaseMeeting == null)
                return false;
            return RequisitionItem.Equals(other.RequisitionItem)
                   && WeekType == other.WeekType
                   && Equals(GroupsChoice, other.GroupsChoice)
                   && Classroom == other.Classroom
                   && Equals(MeetingTime, other.MeetingTime);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Meeting) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(RequisitionItem, WeekType, GroupsChoice, Classroom, MeetingTime);
        }

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
    }
}