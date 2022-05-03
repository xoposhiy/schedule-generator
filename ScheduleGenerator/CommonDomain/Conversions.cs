using CommonDomain.Enums;

namespace CommonDomain;

public static class Conversions
{
    public static readonly Dictionary<string, DayOfWeek> WeekDaysDict = new()
    {
        {"пн", DayOfWeek.Monday},
        {"вт", DayOfWeek.Tuesday},
        {"ср", DayOfWeek.Wednesday},
        {"чт", DayOfWeek.Thursday},
        {"пт", DayOfWeek.Friday},
        {"сб", DayOfWeek.Saturday},
        {"вс", DayOfWeek.Sunday}
    };

    public static MeetingType GetMeetingType(string name)
    {
        return name switch
        {
            "Лекция" => MeetingType.Lecture,
            "КомпПрактика" => MeetingType.ComputerLab,
            "Семинар" => MeetingType.Seminar,
            _ => throw new FormatException($"Некорректный тип занятия: {name}")
        };
    }
}