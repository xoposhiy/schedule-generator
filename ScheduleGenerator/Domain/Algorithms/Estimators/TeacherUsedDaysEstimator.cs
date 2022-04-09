using System;
using CommonDomain;
using CommonDomain.Enums;
using Infrastructure;
using static Domain.DomainExtensions;

namespace Domain.Algorithms.Estimators
{
    public class TeacherUsedDaysEstimator : IEstimator
    {
        private const int MaxTeacherDays = 2;
        private const int MaxTeacherPenalty = WeekTypesCount * 4; // maxExtraDays

        public double EstimateMeetingToAdd(Schedule schedule, Meeting meetingToAdd)
        {
            var teacher = meetingToAdd.Teacher;
            var weekTypes = meetingToAdd.WeekType.GetWeekTypes();
            var affectedDay = meetingToAdd.MeetingTime!.Day;

            if (!schedule.TeacherMeetingsByTime.TryGetValue(teacher, out var byTeacher))
                return 0;

            var penaltyDelta = 0d;
            var maxPenalty = GetMaxPenalty(schedule);

            foreach (var weekType in weekTypes)
            {
                var daysCountBefore = 0;
                var daysCountAfter = 0;

                if (!byTeacher.TryGetValue(weekType, out var byWeekType))
                    continue;

                foreach (var (dayOfWeek, day) in byWeekType)
                {
                    if (day.MeetingsCount() != 0)
                    {
                        daysCountBefore++;
                    }
                    else
                    {
                        if (dayOfWeek == affectedDay)
                            daysCountAfter++;
                    }
                }

                if (!byWeekType.ContainsKey(affectedDay)) daysCountAfter++;

                daysCountAfter += daysCountBefore;

                var extraDaysBefore = GetPenalty(daysCountBefore);
                var extraDaysAfter = GetPenalty(daysCountAfter);

                penaltyDelta += extraDaysAfter - extraDaysBefore;
            }

            return -penaltyDelta / maxPenalty;
        }

        public double Estimate(Schedule schedule, ILogger? logger = null)
        {
            var penalty = 0d;
            var maxPenalty = GetMaxPenalty(schedule);

            foreach (var (teacher, byTeacher) in schedule.TeacherMeetingsByTime)
            foreach (var (weekType, byWeekType) in byTeacher)
            {
                var days = 0;
                foreach (var day in byWeekType.Values)
                    if (day.MeetingsCount() != 0)
                        days++;

                var extraDays = GetPenalty(days);
                if (extraDays == 0) continue;
                logger?.Log(GetLogMessage(teacher, extraDays, weekType), -extraDays / maxPenalty);
                penalty += extraDays;
            }

            return -penalty / maxPenalty;
        }

        private static string GetLogMessage(Teacher? teacher, int extraDays, WeekType weekType)
        {
            var weekTypeString = weekType.GetPrettyString();
            // var dayString = day.GetPrettyString();
            return $"{teacher} has {extraDays} extra days at {weekTypeString} week";
        }

        private static double GetMaxPenalty(Schedule schedule)
        {
            return schedule.TeacherMeetingsByTime.Count * MaxTeacherPenalty;
        }

        private static int GetPenalty(int days)
        {
            return Math.Max(0, days - MaxTeacherDays);
        }
    }
}