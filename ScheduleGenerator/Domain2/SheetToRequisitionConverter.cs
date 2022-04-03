using System.Globalization;
using System.Text.RegularExpressions;
using CommonDomain;
using CommonDomain.Enums;
using Infrastructure;
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

    private static MeetingType GetMeetingType(string name)
    {
        return name switch
        {
            "Лекция" => MeetingType.Lecture,
            "КомпПрактика" => MeetingType.ComputerLab,
            "Семинар" => MeetingType.Seminar,
            _ => throw new FormatException($"Некорректный тип занятия: {name}")
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

    private static RoomSpec GetRoomSpec(string name)
    {
        return name switch
        {
            "компьютеры" => RoomSpec.Computer,
            "проектор" => RoomSpec.Projector,
            "большая" => RoomSpec.Big,
            "на группу" => RoomSpec.ForGroup,
            _ => throw new FormatException($"Некорректный тип аудитории: {name}")
        };
    }

    public static (List<Meeting2>, List<Room>)
        ConvertToRequisitions(GsRepository repo,
            string meetingsSheetName, string classroomsSheetName)
    {
        var dataRaw = SheetTableReader.ReadRowsFromSheet(repo, meetingsSheetName, 0, 0, 16);
        var positions = new Dictionary<string, int>();
        for (int i = 0; i < dataRaw[0].Count; i++)
        {
            positions[dataRaw[0][i]] = i;
        }

        foreach (var row in dataRaw.Skip(1))
        {
            var discipline = new Discipline(row[positions["Discipline"]]);
            var meetingType = GetMeetingType(row[positions["MeetingType"]]);
            var teacher = new Teacher(row[positions["Teacher"]]);
            var groups = ParseGroups(row[positions["Groups"]]);
            var place = row[positions["Place"]];
            var roomSpecs = ParseRoomSpec(row[positions["RoomSpecs"]]);
            var duration = ParseDuration(row[positions["Duration"]]);
        }



        throw new NotImplementedException();
    }

    private static int ParseDuration(string raw)
    {
        return string.IsNullOrEmpty(raw) ? 1 : int.Parse(raw);
    }

    private static List<int> ParseGroups(string raw)
    {
        return raw.Split(',').Select(int.Parse).ToList();
    }

    private static RoomSpec[] ParseRoomSpec(string raw)
    {
        return string.IsNullOrWhiteSpace(raw)
            ? Array.Empty<RoomSpec>()
            : raw.Split(',')
                .Select(mgs => mgs.Trim())
                .Select(GetRoomSpec).ToArray();
    }

    private static List<Room> ParseClassrooms(IEnumerable<List<string>> sheetData)
    {
        // var list = new List<RoomRequisition>();
        // foreach (var (room, specs, lockedTimes) in sheetData.Select(ParseClassroom))
        //     list.Add(new(room, specs, lockedTimes));
        // return list;
        throw new NotImplementedException();
    }

    private static (string room, List<RoomSpec> specs, HashSet<MeetingTime> lockedTimes) ParseClassroom(
        List<string> row)
    {
        // var number = row[0];
        // var specs = new List<RoomSpec>();
        // if (!string.IsNullOrWhiteSpace(row[1]))
        //     specs.Add(RoomSpec.Computer);
        // if (!string.IsNullOrWhiteSpace(row[2]))
        //     specs.Add(RoomSpec.Projector);
        // if (!string.IsNullOrWhiteSpace(row[3]))
        //     specs.Add(GetRoomSpec(row[3]));
        // var lockedTimes = ParseRoomsTimeRequisitions(row[4]);
        // return (number, specs, lockedTimes);
        throw new NotImplementedException();
    }

    private static WeekType ParseWeekType(string? weekTypeRaw)
    {
        return string.IsNullOrEmpty(weekTypeRaw) ? WeekType.All : GetWeekType(weekTypeRaw);
    }

    public static List<MeetingTime> ParseMeetingTimeRequisitions(string rawMeetingTime)
    {
        // if (!string.IsNullOrWhiteSpace(rawMeetingTime)) return ParseTimes(rawMeetingTime);
        // var meetingTimes = GetAllPossibleMeetingTimes().ToHashSet();
        // return new() {new(meetingTimes)};
        throw new NotImplementedException();
    }

    private static IEnumerable<MeetingTime> ParseTimes(string rawMeetingTime)
    {
        // try
        // {
        //     var meetingTimeRequisitions = new List<MeetingTimeRequisition>();
        //
        //     var records = rawMeetingTime.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        //
        //     foreach (var record in records)
        //     {
        //         var meetingTimes = new HashSet<MeetingTime>();
        //
        //         var blocks = record.Split(';', StringSplitOptions.RemoveEmptyEntries);
        //         foreach (var block in blocks)
        //         {
        //             var parts = block.Replace(" ", "").Split(':');
        //
        //             var days = GetDays(parts[0]);
        //             var slots = parts.Length > 1 ? GetSlots(parts[1]) : GetSlots("1-6 пары");
        //             foreach (var day in days)
        //             foreach (var slot in slots)
        //                 meetingTimes.Add(new(day, slot));
        //         }
        //
        //         meetingTimeRequisitions.Add(new(meetingTimes));
        //     }
        //
        //     return meetingTimeRequisitions;
        // }
        // catch (Exception e)
        // {
        //     throw new Exception($"Can't parse time '{rawMeetingTime}'", e);
        // }
        throw new NotImplementedException();
    }

    private static List<DayOfWeek> GetDays(string dayString)
    {
        var days = new List<DayOfWeek>();
        var dayReqs = dayString.Split(',');
        foreach (var req in dayReqs)
        {
            var tmp = req.Split('-');
            var firstDay = WeekDaysDict[tmp[0]];
            var lastDay = WeekDaysDict[tmp[^1]];
            for (var day = firstDay; day <= lastDay; day++)
                days.Add(day);
        }

        return days;
    }

    private static List<int> GetSlots(string slotString)
    {
        var slots = new List<int>();
        var slotReqs = slotString.Split(',');
        foreach (var req in slotReqs)
        {
            //пн-пт: 3,4 пара
            var tmp = req.Split('-');
            var firstSlot = int.Parse(tmp[0][0].ToString());
            var lastSlot = tmp.Length == 1 ? firstSlot : int.Parse(tmp[1][0].ToString());
            if (firstSlot < 1 || lastSlot > 6)
                throw new FormatException("meeting slots must be between 1 and 6");
            for (var slot = firstSlot; slot <= lastSlot; slot++)
                slots.Add(slot);
        }

        return slots;
    }
}