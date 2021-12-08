using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Enums;
using Domain.MeetingsParts;
using Infrastructure;
using static Domain.DomainExtensions;

namespace Domain.Algorithms.Estimators.GroupsEstimators
{
    public class LocationPerDayEstimator : GroupEstimator
    {
        private const int OptimalLocationsChangesCount = 1;
        private const int PossibleMaximumLocationsCount = 4;

        public override double GetScoreByGroup(MeetingGroup group, Schedule schedule, ILogger? logger = null)
        {
            var byGroup = schedule.GroupMeetingsByTime[group];
            var penalty = 0;
            var scorePart = -1 / GetMaxPenalty(schedule);

            foreach (var (weekType, byWeekType) in byGroup)
            foreach (var (day, byDay) in byWeekType)
            {
                var locations = byDay.Select(m => m?.Location);
                var penaltyPart = GetPenalty(locations);
                if (penaltyPart == 0) continue;
                penalty += penaltyPart;
                logger?.Log(GetLogMessage(group, weekType, day, penaltyPart), scorePart * penaltyPart);
            }

            return penalty * scorePart;
        }

        public override double EstimateMeetingToAdd(Schedule schedule, Meeting meetingToAdd)
        {
            var penaltyDelta = 0;
            var maxPenalty = GetMaxPenalty(schedule);

            var groups = meetingToAdd.GroupsChoice!.GetGroupParts();
            var weekTypes = meetingToAdd.WeekType.GetWeekTypes();

            var (dayOfWeek, timeSlot) = meetingToAdd.MeetingTime!;

            foreach (var meetingGroup in groups)
            foreach (var weekType in weekTypes)
            {
                var locations = new Location?[7];
                if (schedule.GroupMeetingsByTime.TryGetValue(meetingGroup, weekType, dayOfWeek, out var byDay))
                {
                    if (byDay[timeSlot] != null)
                        throw new AggregateException("Placing meeting in taken place");
                    locations = byDay.Select(m => m?.Location).ToArray();
                }

                var beforePenalty = GetPenalty(locations);
                locations[meetingToAdd.MeetingTime.TimeSlot - 1] = meetingToAdd.Location;
                var afterPenalty = GetPenalty(locations);

                penaltyDelta += afterPenalty - beforePenalty;
            }

            return -penaltyDelta / maxPenalty;
        }

        private static string GetLogMessage(MeetingGroup group, WeekType weekType, DayOfWeek day, int count)
        {
            var weekTypeString = weekType.GetPrettyString();
            var dayString = day.GetPrettyString();
            return $"{group} has bad {weekTypeString} with {count} locations changes on {dayString}";
        }

        public override double GetMaxPenalty(Schedule schedule)
        {
            return schedule.GroupMeetingsByTime.Count * WeekTypesCount * MaxDaysCount * PossibleMaximumLocationsCount;
        }

        private static int GetPenalty(IEnumerable<Location?> meetingsLocations)
        {
            var locations = meetingsLocations.Where(l => l.HasValue).Select(l => l!.Value).ToList();
            var count = 0;
            if (locations.Count == 0) return 0;
            var previous = locations[0];
            for (var i = 1; i < locations.Count; i++)
            {
                if (previous == locations[i]) continue;
                previous = locations[i];
                count++;
            }

            return count > OptimalLocationsChangesCount ? count - OptimalLocationsChangesCount : 0;
        }
    }
}