using System;
using System.Collections.Generic;

namespace Domain.Algorithms.Estimators
{
    public class StudentsSpacesEstimator : IEstimator
    {
        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            throw new NotImplementedException();
        }

        public double Estimate(Schedule schedule, List<string>? logger = null)
        {
            //TODO придумать как учитывать пары, которые идут не весь семестр.
            //Например, учитывать аналогично четным-нечетным неделям (см ниже).
            var penalty = 0d;

            var daysCount = 0;
            var groupsCount = schedule.GroupMeetingsByTime.Count;

            // foreach (var (group, weekType, day, byDay) in schedule.GroupMeetingsByTime.Enumerate())
            foreach (var (group, byGroup) in schedule.GroupMeetingsByTime)
            foreach (var (weekType, byWeekType) in byGroup)
            foreach (var (day, byDay) in byWeekType)
            {
                var spacesCount = byDay.GetMeetingsSpacesCount();
                if (spacesCount != 0) logger?.Add($"{@group} has {spacesCount} spaces on {weekType} {day}");
                penalty += spacesCount;
                daysCount++;
            }

            return -penalty / (groupsCount * daysCount);
        }
    }
}