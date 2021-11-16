using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Enums;
using Domain.MeetingsParts;
using Infrastructure;
using static Domain.DomainExtensions;

namespace Domain.Algorithms.Estimators
{
    public class LocationPerDayEstimator : IEstimator
    {
        private const int OptimalLocationsCount = 1;
        private const int PossibleMaximumLocationsCount = 4;

        public double EstimateMeetingToAdd(Schedule schedule, Meeting meetingToAdd)
        {
            var penaltyDelta = 0;
            var maxPenalty = GetMaxPenalty(schedule);

            var groups = meetingToAdd.GroupsChoice!.GetGroupParts();
            var weekTypes = meetingToAdd.WeekType.GetWeekTypes();

            var (dayOfWeek, timeSlot) = meetingToAdd.MeetingTime!;

            foreach (var meetingGroup in groups)
            foreach (var weekType in weekTypes)
            {
                var locations = new List<Location>();
                if (schedule.GroupMeetingsByTime.TryGetValue(meetingGroup, weekType, dayOfWeek, out var byDay))
                {
                    if (byDay[timeSlot] != null)
                        throw new AggregateException("Placing meeting in taken place");
                    locations.AddRange(byDay.Where(m => m != null).Select(m => m!.Location));
                }

                var beforePenalty = GetPenalty(locations);
                locations.Add(meetingToAdd.Location);
                var afterPenalty = GetPenalty(locations);

                penaltyDelta += afterPenalty - beforePenalty;
            }

            return -penaltyDelta / maxPenalty;
        }

        public double Estimate(Schedule schedule, ILogger? logger = null)
        {
            var penalty = 0;
            var maxPenalty = GetMaxPenalty(schedule);

            foreach (var (group, byGroup) in schedule.GroupMeetingsByTime)
            foreach (var (weekType, byWeekType) in byGroup)
            foreach (var (day, byDay) in byWeekType)
            {
                var count = byDay.Where(m => m != null).Select(m => m!.Location).Distinct().Count();
                penalty += count > OptimalLocationsCount ? count - OptimalLocationsCount : 0;
                if (count <= 1) continue;
                logger?.Log(GetLogMessage(group, weekType, day, count), -1 / maxPenalty);
            }

            return -penalty / maxPenalty;
        }

        private static string GetLogMessage(MeetingGroup group, WeekType weekType, DayOfWeek day, int count)
        {
            var weekTypeString = weekType.ToString().PadRight(4);
            var dayString = day.ToString().PadRight(8);
            return $"{group} has bad {weekTypeString} with {count} locations on {dayString}";
        }

        private static double GetMaxPenalty(Schedule schedule)
        {
            return schedule.GroupMeetingsByTime.Count * WeekTypesCount * MaxDaysCount * PossibleMaximumLocationsCount;
        }

        private static int GetPenalty(IEnumerable<Location> meetingsLocations)
        {
            var count = meetingsLocations.Distinct().Count();
            return count > OptimalLocationsCount ? count - OptimalLocationsCount : 0;
        }
    }
}