using System.Collections.Generic;
using System.Linq;
using Domain.Algorithms;
using Domain.Conversions;

namespace Domain
{
    public class RequisitionItem
    {
        public readonly LearningPlanItem PlanItem;
        public readonly GroupRequisition[] GroupPriorities;
        public readonly Location Location;

        public readonly int RepetitionsCount;

        public readonly MeetingTimeRequisition[] MeetingTimePriorities;
        public readonly Teacher Teacher;
        public readonly WeekType WeekType;

        public RequisitionItem(LearningPlanItem planItem, string groupPriorities,
            string meetingTimePriorities, Teacher teacher, int repetitionsCount = 1,
            Location location = Location.MathMeh, WeekType weekType = WeekType.All)
            : this(planItem,
                SheetToRequisitionConverter.ParseGroupRequisitions(groupPriorities).ToArray(),
                repetitionsCount,
                SheetToRequisitionConverter.ParseMeetingTimeRequisitions(meetingTimePriorities).ToArray(),
                teacher, location, weekType)
        {
        }

        public RequisitionItem(LearningPlanItem planItem, GroupRequisition[] groupPriorities, int repetitionsCount,
            MeetingTimeRequisition[] meetingTimePriorities, Teacher teacher, Location location,
            WeekType weekType = WeekType.All)
        {
            PlanItem = planItem;
            GroupPriorities = groupPriorities;
            RepetitionsCount = repetitionsCount;
            MeetingTimePriorities = meetingTimePriorities;
            Teacher = teacher;
            WeekType = weekType;
            Location = location;
        }


        public override string ToString()
        {
            return $"{string.Join(" ", GroupPriorities.ToList())}, {Teacher}"; //, {}, {}, {}";
        }

        public RequisitionItem Copy()
        {
            return new(PlanItem, GroupPriorities, RepetitionsCount,
                MeetingTimePriorities, Teacher, Location, WeekType);
        }
    }

    public record GroupRequisition(GroupsChoice[] GroupsChoices)
    {
        public override string ToString()
        {
            return string.Join("; ", GroupsChoices.ToList());
        }
    }

    public record MeetingTimeRequisition(MeetingTime[] MeetingTimeChoices);

    public sealed record GroupsChoice(MeetingGroup[] Groups)
    {
        public IEnumerable<MeetingGroup> GetGroupParts()
        {
            return Groups.GetGroupParts();
        }

        public override string ToString()
        {
            return string.Join(" ", Groups.ToList());
        }
    }
}