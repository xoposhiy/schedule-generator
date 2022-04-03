using CommonDomain;
using CommonDomain.Enums;
using Infrastructure.GoogleSheetsRepository;
using Infrastructure.SheetPatterns;
// ReSharper disable MemberCanBePrivate.Global

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
        return rowWeekType.ToLower() switch
        {
            "чет" => WeekType.Even,
            "нечет" => WeekType.Odd,
            "четная" => WeekType.Even,
            "нечетная" => WeekType.Odd,
            _ => throw new FormatException($"Некорректная четность недели: {rowWeekType}")
        };
    }

    private static RoomSpec GetRoomSpec(string name)
    {
        return name.ToLower() switch
        {
            "компьютеры" => RoomSpec.Computer,
            "комп" => RoomSpec.Computer,
            "проектор" => RoomSpec.Projector,
            "большая" => RoomSpec.Big,
            "на группу" => RoomSpec.ForGroup,
            _ => throw new FormatException($"Некорректный тип аудитории: {name}")
        };
    }

    public static List<Meeting2> ConvertToRequisitions(GsRepository repo, string meetingsSheetName, string classroomsSheetName)
    {
        var dataRaw = SheetTableReader.ReadRowsFromSheet(repo, meetingsSheetName, 0, 0, 16);
        var positions = new Dictionary<string, int>();
        for (int i = 0; i < dataRaw[0].Count; i++)
        {
            positions[dataRaw[0][i]] = i;
        }

        var meetings = new List<Meeting2>();
        foreach (var row in dataRaw.Skip(1))
        {
            var discipline = new Discipline(row[positions["Discipline"]]);
            var meetingType = GetMeetingType(row[positions["MeetingType"]]);
            var teacher = new Teacher(row[positions["Teacher"]]);
            var groups = ParseGroups(row[positions["Groups"]]);
            var place = row[positions["Place"]];
            var roomSpecs = ParseRoomSpec(row[positions["RoomSpecs"]]);
            var duration = ParseInt(row[positions["Duration"]], 1);
            var weekTypeSpec = ParseWeekType(row[positions["WeekTypeSpec"]]);
            var meetingTimePriorities = ParseMeetingTimePriorities(row[positions["MeetingTimePriorities"]]); //TODO пропихнуть четность
            var after = string.IsNullOrEmpty(row[positions["After"]]) ? (MeetingType?)null : GetMeetingType(row[positions["After"]]);
            var hasEntranceTest = Convert.ToBoolean(ParseInt(row[positions["HasEntranceTest"]], 0));
            var priority = ParseInt(row[positions["Priority"]], 2);   //TODO разобраться с дефолтным приоритетом
            var isFixed = Convert.ToBoolean(ParseInt(row[positions["IsFixed"]], 0));
            var shouldBePlaced = Convert.ToBoolean(ParseInt(row[positions["ShouldBePlaced"]], 1));

            var classRoom = string.IsNullOrEmpty(row[positions["ClassRoom"]]) ? null : row[positions["ClassRoom"]];
            var time = ParseMeetingTime(row[positions["Time"]]).FirstOrDefault((MeetingTime?)null);
            meetings.Add(new Meeting2(discipline, meetingType, teacher, groups, place, roomSpecs, duration,
                weekTypeSpec, meetingTimePriorities, after, hasEntranceTest, priority, isFixed, shouldBePlaced, classRoom, time));
        }

        return meetings;
    }

    private static int ParseInt(string raw, int defaultValue)
    {
        return string.IsNullOrEmpty(raw) ? defaultValue : int.Parse(raw);
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

    public static List<MeetingTime> ParseMeetingTimePriorities(string rawMeetingTime)
    {
        if (!string.IsNullOrWhiteSpace(rawMeetingTime)) return ParseMeetingTimes(rawMeetingTime);
        return GetAllPossibleMeetingTimes();
    }

    public static List<MeetingTime> GetAllPossibleMeetingTimes()
    {
        var ans = new List<MeetingTime>();
        foreach (var weekType in (WeekType[])Enum.GetValues(typeof(WeekType)))
        {
            foreach (var dayOfWeek in (DayOfWeek[])Enum.GetValues(typeof(DayOfWeek)))
            {
                for (var timeslot = 1; timeslot <= Constants.TimeSlots; timeslot++)
                {
                    ans.Add(new MeetingTime(weekType, dayOfWeek, timeslot));
                }
            }
        }

        return ans;
    }

    private static List<MeetingTime> ParseMeetingTimes(string raw)
    {
        try
        {
            var meetingTimePriorities = new List<MeetingTime>();
        
            var lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
            foreach (var line in lines)
            {
                foreach (var meetingTime in ParseMeetingTime(line))
                {
                    meetingTimePriorities.Add(meetingTime);
                }
            }
        
            return meetingTimePriorities;
        }
        catch (Exception e)
        {
            throw new Exception($"Can't parse time '{raw}'", e);
        }
    }

    private static IEnumerable<MeetingTime> ParseMeetingTime(string line)
    {
        var compactedLine = line.Replace(" ", "");
        if (string.IsNullOrEmpty(compactedLine))
            yield break;
        var parts = compactedLine.Split(':');

        var days = GetDays(parts[0]);
        var slots = parts.Length > 1 ? GetSlots(parts[1]) : GetSlots($"1-{Constants.TimeSlots}");
        foreach (var day in days)
            foreach (var slot in slots)
                yield return new MeetingTime(WeekType.All, day, slot); //TODO: добавить четность
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
            if (firstSlot < 1 || lastSlot > Constants.TimeSlots)
                throw new FormatException($"meeting slots must be between 1 and {Constants.TimeSlots}");
            for (var slot = firstSlot; slot <= lastSlot; slot++)
                slots.Add(slot);
        }

        return slots;
    }
}