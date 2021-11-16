using System;
using Domain.Enums;
using Domain.MeetingsParts;
using Infrastructure;
using static Domain.DomainExtensions;

namespace Domain.Algorithms.Estimators
{
    public class StudentsSpacesEstimator : IEstimator
    {
        public double EstimateMeetingToAdd(Schedule schedule, Meeting meetingToAdd)
        {
            var groups = meetingToAdd.GroupsChoice!.GetGroupParts();

            var penaltyDelta = 0d;
            var maxPenalty = GetMaxPenalty(schedule);

            foreach (var meetingGroup in groups)
                penaltyDelta += GetSpacesCountDelta(meetingToAdd, meetingGroup, schedule.GroupMeetingsByTime);

            return -penaltyDelta / maxPenalty;
        }

        public double Estimate(Schedule schedule, ILogger? logger = null)
        {
            //TODO придумать как учитывать пары, которые идут не весь семестр.
            //Например, учитывать аналогично четным-нечетным неделям (см ниже).
            var penalty = 0d;
            var maxPenalty = GetMaxPenalty(schedule);

            foreach (var (group, byGroup) in schedule.GroupMeetingsByTime)
            foreach (var (weekType, byWeekType) in byGroup)
            foreach (var (day, byDay) in byWeekType)
            {
                var spacesCount = byDay.GetMeetingsSpacesCount();
                if (spacesCount == 0) continue;
                logger?.Log(GetLogMessage(group, weekType, day, spacesCount), -spacesCount / maxPenalty);
                penalty += spacesCount;
            }

            return -penalty / maxPenalty;
        }

        private static string GetLogMessage(MeetingGroup group, WeekType weekType, DayOfWeek day, int spacesCount)
        {
            var weekTypeString = weekType.ToString().PadRight(4);
            var dayString = day.ToString().PadRight(8);
            return $"{group} has {spacesCount} spaces on {weekTypeString} {dayString}";
        }

        private static double GetMaxPenalty(Schedule schedule)
        {
            return schedule.GroupMeetingsByTime.Count * MaxSpaces;
        }
    }
}