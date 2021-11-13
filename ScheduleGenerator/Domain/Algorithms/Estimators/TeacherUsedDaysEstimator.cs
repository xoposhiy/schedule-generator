using System;
using Infrastructure;

namespace Domain.Algorithms.Estimators
{
    public class TeacherUsedDaysEstimator : IEstimator
    {
        private const int MaxTeacherDays = 2;
        private const int MaxWeekTypesSpaces = 2 * 4; // weekTypes * maxExtraDays
        
        public double EstimateMeetingToAdd(Schedule schedule, Meeting meetingToAdd)
        {
            var teacher = meetingToAdd.Teacher;
            var weekTypes = meetingToAdd.WeekType.GetWeekTypes();
            var dayOfWeek = meetingToAdd.MeetingTime!.Day;

            var daysCount = 0;

            if (!schedule.TeacherMeetingsByTime.TryGetValue(teacher, out var byTeacher)) return 0;
            foreach (var weekType in weekTypes)
            {
                if (!byTeacher.TryGetValue(weekType, out var byWeekType))
                {
                    daysCount++;
                    continue;
                }

                if (!byWeekType.TryGetValue(dayOfWeek, out var day)) continue;
                if (day.MeetingsCount() == 0) continue;
                daysCount++;
            }

            var extraDays = Math.Max(0, daysCount - MaxTeacherDays);

            return -(double) extraDays / MaxWeekTypesSpaces;
        }

        public double Estimate(Schedule schedule, ILogger? logger = null)
        {
            var penalty = 0d;
            double maxPenalty = schedule.TeacherMeetingsByTime.Count * MaxWeekTypesSpaces; 

            foreach (var (teacher, byTeacher) in schedule.TeacherMeetingsByTime)
            foreach (var (weekType, byWeekType) in byTeacher)
            {
                var days = 0;
                foreach (var day in byWeekType.Values)
                    if (day.MeetingsCount() != 0)
                        days++;

                var extraDays = Math.Max(0, days - MaxTeacherDays);
                if (extraDays == 0) continue;
                logger?.Log($"{teacher} has {extraDays} extra days at {weekType} week", -extraDays / maxPenalty);
                penalty += extraDays;
            }

            return -penalty / maxPenalty;
        }
    }
}