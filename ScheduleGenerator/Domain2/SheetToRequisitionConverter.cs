using System.Globalization;
using System.Text.RegularExpressions;
using CommonDomain;
using CommonDomain.Enums;
using Infrastructure.GoogleSheetsRepository;
using Infrastructure.SheetPatterns;

namespace Domain2;

public static class SheetToRequisitionConverter
{
    public static readonly Dictionary<string, DayOfWeek> WeekDaysDict = new()
    {
        {"пн", DayOfWeek.Monday},
        {"вт", DayOfWeek.Tuesday},
        {"ср", DayOfWeek.Wednesday},
        {"чт", DayOfWeek.Thursday},
        {"пт", DayOfWeek.Friday},
        {"сб", DayOfWeek.Saturday}
    };

    public static Location StringToLocation(string place) => new Location(place);

    private static MeetingType GetMeetingType(string rowMeetingType)
    {
        return rowMeetingType switch
        {
            "Лекция" => MeetingType.Lecture,
            "КомпПрактика" => MeetingType.ComputerLab,
            "Семинар" => MeetingType.Seminar,
            _ => throw new FormatException($"Некорректный тип занятия: {rowMeetingType}")
        };
    }

    private static WeekType GetWeekType(string rowWeekType)
    {
        return rowWeekType switch
        {
            "чет" => WeekType.Even,
            "нечет" => WeekType.Odd,
            _ => throw new FormatException($"Некорректная четность недели: {rowWeekType}")
        };
    }

    private static RoomSpec GetRoomSpec(string rowRoomSpec)
    {
        return rowRoomSpec switch
        {
            "компьютеры" => RoomSpec.Computer,
            "проектор" => RoomSpec.Projector,
            "большая" => RoomSpec.Big,
            "на группу" => RoomSpec.ForGroup,
            _ => throw new FormatException($"Некорректный тип аудитории: {rowRoomSpec}")
        };
    }

    public static (List<Meeting2>, List<Room>)
        ConvertToRequisitions(GsRepository repo,
            string requisitionSheetName, string learningPlanSheetName, string classroomsSheetName)
    {
        throw new NotImplementedException();
    }

    
}