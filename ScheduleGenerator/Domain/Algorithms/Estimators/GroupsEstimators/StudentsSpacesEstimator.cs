using System;
using Domain.Enums;
using Domain.MeetingsParts;
using Infrastructure;
using static Domain.DomainExtensions;

namespace Domain.Algorithms.Estimators.GroupsEstimators
{
    public class StudentsSpacesEstimator : GroupEstimator
    {
        public override double GetScoreByGroup(MeetingGroup @group, Schedule schedule, ILogger? logger = null)
        {
            //TODO придумать как учитывать пары, которые идут не весь семестр.
            //Например, учитывать аналогично четным-нечетным неделям (см ниже).
            var byGroup = schedule.GroupMeetingsByTime[group];
            var penalty = 0;
            var scorePart = -1 / GetMaxPenalty(schedule);

            foreach (var (weekType, byWeekType) in byGroup)
            foreach (var (day, byDay) in byWeekType)
            {
                var spacesCount = byDay.GetMeetingsSpacesCount();
                if (spacesCount == 0) continue;
                logger?.Log(GetLogMessage(group, weekType, day, spacesCount), spacesCount * scorePart);
                penalty += spacesCount;
            }

            return penalty * scorePart;
        }

        public override double EstimateMeetingToAdd(Schedule schedule, Meeting meetingToAdd)
        {
            var groups = meetingToAdd.GroupsChoice!.GetGroupParts();

            var penaltyDelta = 0d;
            var maxPenalty = GetMaxPenalty(schedule);

            foreach (var meetingGroup in groups)
                penaltyDelta += GetSpacesCountDelta(meetingToAdd, meetingGroup, schedule.GroupMeetingsByTime);

            return -penaltyDelta / maxPenalty;
        }

        private static string GetLogMessage(MeetingGroup group, WeekType weekType, DayOfWeek day, int spacesCount)
        {
            var weekTypeString = weekType.GetPrettyString();
            var dayString = day.GetPrettyString();
            return $"{group} has {spacesCount} spaces on {weekTypeString} {dayString}";
        }

        public override double GetMaxPenalty(Schedule schedule)
        {
            return schedule.GroupMeetingsByTime.Count * MaxSpaces;
        }
    }
}