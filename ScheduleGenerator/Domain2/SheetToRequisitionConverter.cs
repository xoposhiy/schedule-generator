using System.Globalization;
using System.Text.RegularExpressions;
using CommonDomain;
using CommonDomain.Enums;
using Infrastructure.GoogleSheetsRepository;
using Infrastructure.SheetPatterns;

namespace Domain2
{
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

        
    }
}