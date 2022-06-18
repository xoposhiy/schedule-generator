using CommonDomain.Enums;

namespace Domain2;

/// <summary>
/// Время проведения пары
/// </summary>
/// <param name="WeekType">Тип недели, когда надо проводить пару: четная/нечетная или каждая</param>
/// <param name="DayOfWeek">День недели</param>
/// <param name="TimeSlot">Номер стандартного начала пары</param>
public record MeetingTime(WeekType WeekType, DayOfWeek DayOfWeek, int TimeSlot)
{
    public int TimeSlotIndex => TimeSlot - 1;

    public override string ToString()
    {
        var dayString = CommonDomain.Conversions.WeekDaysDict.FirstOrDefault(pair => pair.Value == DayOfWeek).Key;
        var res = $"{dayString} {TimeSlot}";
        if (WeekType != WeekType.All)
            res += $" {WeekType.ToRuString()[..^3]}";
        return res;
    }
}