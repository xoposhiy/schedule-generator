using CommonDomain.Enums;

namespace Domain2;

public record MeetingTime(WeekType WeekType, DayOfWeek DayOfWeek, int TimeSlot)
{
    public override string ToString()
    {
        var dayString = CommonDomain.Conversions.WeekDaysDict.FirstOrDefault(pair => pair.Value == DayOfWeek).Key;
        var weekString = WeekType == WeekType.All ? "" : WeekType.ToRuString()[..3];
        return $"{dayString}:{TimeSlot}:{weekString}";
    }
}