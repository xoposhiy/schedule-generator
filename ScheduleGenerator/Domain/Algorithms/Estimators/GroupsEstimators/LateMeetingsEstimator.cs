using System;
using CommonDomain.Enums;
using Domain.MeetingsParts;
using Infrastructure;

namespace Domain.Algorithms.Estimators.GroupsEstimators;

public class LateMeetingsEstimator : GroupEstimator
{
    public override double GetMaxPenalty(Schedule schedule)
    {
        return (schedule.Meetings.Count + schedule.NotUsedMeetings.Count); //TODO:(mexbandoc) посчитать нормально максимальный штраф
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
            if (meeting!.MeetingTime!.TimeSlot <= 6) continue;
            penalty++;
            logger?.Log(GetLogMessage(@group, weekType, day, meeting), scorePart);
        }
        
        return penalty * scorePart;
    }

    public override double EstimateMeetingToAdd(Schedule schedule, Meeting meetingToAdd)
    {
        throw new System.NotImplementedException(); //TODO:(mexbandoc) сделать
    }

    private static string GetLogMessage(MeetingGroup group, WeekType weekType, DayOfWeek day, Meeting meeting)
    {
        var weekTypeString = weekType.GetPrettyString();
        var dayString = day.GetPrettyString();
        return $"{group} has bad {weekTypeString} {dayString} with meeting \"{meeting.Discipline} {meeting.Teacher}\" at {meeting.MeetingTime!.TimeSlot} slot";
    }
}