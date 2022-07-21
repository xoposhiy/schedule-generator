using System;
using CommonDomain.Enums;
using Domain.MeetingsParts;
using Infrastructure;
using static Domain.DomainExtensions;

namespace Domain.Algorithms.Estimators.GroupsEstimators;

public class LateMeetingsEstimator : GroupEstimator
{
    private const int MaxLateMeetingsPerDayCount = 2;
    private const int NotLateMeetingsPerDayCount = 6;
    
    public override double GetMaxPenalty(Schedule schedule)
    {
        return schedule.GroupMeetingsByTime.Count * WeekTypesCount * 
               Math.Min(schedule.Meetings.Count + schedule.NotUsedMeetings.Count, 
                   MaxDaysCount * MaxLateMeetingsPerDayCount);
    }

    public override double GetScoreByGroup(MeetingGroup group, Schedule schedule, ILogger? logger = null)
    {
        var byGroup = schedule.GroupMeetingsByTime[group];
        var penalty = 0;
        var scorePart = -1 / GetMaxPenalty(schedule);

        foreach (var (weekType, byWeekType) in byGroup)
        foreach (var (day, byDay) in byWeekType)
        foreach (var meeting in byDay)
        {
            if (meeting == null || meeting.MeetingTime!.TimeSlot <= NotLateMeetingsPerDayCount) continue;
            penalty++;
            logger?.Log(GetLogMessage(@group, weekType, day, meeting), scorePart);
        }
        
        return penalty * scorePart;
    }

    public override double EstimateMeetingToAdd(Schedule schedule, Meeting meetingToAdd)
    {
        var penalty = meetingToAdd.MeetingTime!.TimeSlot > NotLateMeetingsPerDayCount ? 1 : 0;
        var maxPenalty = GetMaxPenalty(schedule);
        
        var groups = meetingToAdd.GroupsChoice!.GetGroupParts();
        var weekTypes = meetingToAdd.WeekType.GetWeekTypes();

        var penaltyDelta = penalty * groups.Count * weekTypes.Length;
        return -penaltyDelta / maxPenalty;
    }

    private static string GetLogMessage(MeetingGroup group, WeekType weekType, DayOfWeek day, Meeting meeting)
    {
        var weekTypeString = weekType.GetPrettyString();
        var dayString = day.GetPrettyString();
        return $"{group} has bad {weekTypeString} {dayString} with meeting \"{meeting.Discipline} {meeting.Teacher}\" at {meeting.MeetingTime!.TimeSlot} slot";
    }
}