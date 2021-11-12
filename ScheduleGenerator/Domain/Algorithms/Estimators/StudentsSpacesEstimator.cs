using Infrastructure;
using static Domain.DomainExtensions;

namespace Domain.Algorithms.Estimators
{
    public class StudentsSpacesEstimator : IEstimator
    {
        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            var groups = meetingToAdd.GroupsChoice!.GetGroupParts();

            var penalty = 0d;

            foreach (var meetingGroup in groups)
                penalty += GetSpacesPenalty(meetingToAdd, meetingGroup, schedule.GroupMeetingsByTime);

            var maxPenalty = groups.Count;

            return -penalty / maxPenalty;
        }

        public double Estimate(Schedule schedule, ILogger? logger = null)
        {
            //TODO придумать как учитывать пары, которые идут не весь семестр.
            //Например, учитывать аналогично четным-нечетным неделям (см ниже).
            var penalty = 0d;

            double maxPenalty = schedule.GroupMeetingsByTime.Count * MaxSpaces;

            foreach (var (group, byGroup) in schedule.GroupMeetingsByTime)
            foreach (var (weekType, byWeekType) in byGroup)
            foreach (var (day, byDay) in byWeekType)
            {
                var spacesCount = byDay.GetMeetingsSpacesCount();
                if (spacesCount == 0) continue;
                logger?.Log($"{group} has {spacesCount} spaces on {weekType} {day}", -spacesCount / maxPenalty);
                penalty += spacesCount;
            }

            return -penalty / maxPenalty;
        }
    }
}