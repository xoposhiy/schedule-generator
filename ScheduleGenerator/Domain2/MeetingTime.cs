using CommonDomain.Enums;

namespace Domain2;

public record MeetingTime(WeekType WeekType, DayOfWeek DayOfWeek, int TimeSlot)
{
    public override string ToString()
    {
        var dayString = CommonDomain.Conversions.WeekDaysDict.FirstOrDefault(pair => pair.Value == DayOfWeek).Key;
        var res = $"{dayString} {TimeSlot}";
        if (WeekType != WeekType.All)
            res += $" {WeekType.ToRuString()[..3]}";
        return res;
    }

    public int TimeSlotIndex => TimeSlot - 1;
}
