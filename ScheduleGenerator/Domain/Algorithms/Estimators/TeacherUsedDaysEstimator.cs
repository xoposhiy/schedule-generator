using System;
using Infrastructure;

namespace Domain.Algorithms.Estimators
{
    public class TeacherUsedDaysEstimator : IEstimator
    {
        private const int MaxTeacherDays = 2;
        private const int MaxTeacherPenalty = 2 * 4; // weekTypes * maxExtraDays
        
        public double EstimateMeetingToAdd(Schedule schedule, Meeting meetingToAdd)
        {
            var teacher = meetingToAdd.Teacher;
            var weekTypes = meetingToAdd.WeekType.GetWeekTypes();
            var affectedDay = meetingToAdd.MeetingTime!.Day;
            
            if (!schedule.TeacherMeetingsByTime.TryGetValue(teacher, out var byTeacher)) 
                return 0;
            
            double maxPenalty = schedule.TeacherMeetingsByTime.Count * MaxTeacherPenalty;
            var penaltyDelta = 0d;
            
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

                var extraDaysBefore = Math.Max(0, daysCountBefore - MaxTeacherDays);
                var extraDaysAfter = Math.Max(0, daysCountAfter - MaxTeacherDays);

                penaltyDelta += extraDaysAfter - extraDaysBefore;
            }

            return -penaltyDelta / maxPenalty;
        }

        public double Estimate(Schedule schedule, ILogger? logger = null)
        {
            var penalty = 0d;
            double maxPenalty = schedule.TeacherMeetingsByTime.Count * MaxTeacherPenalty; 

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