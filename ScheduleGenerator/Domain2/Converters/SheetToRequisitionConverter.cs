using System.Text.RegularExpressions;
using CommonDomain;
using CommonDomain.Enums;
using CommonInfrastructure;
using CommonInfrastructure.GoogleSheetsRepository;
using static CommonDomain.Conversions;

// ReSharper disable MemberCanBePrivate.Global

namespace Domain2.Converters;

//TODO парсить кабинеты

public static class SheetToRequisitionConverter
{
    public static Location StringToLocation(string place) => new Location(place);
    public static readonly Regex DayOfWeekRegex = new Regex("пн|вт|ср|чт|пт|сб");
    public static readonly Regex WeekTypeRegex = new Regex("чет|нечет");
    public static readonly Regex NumberRegex = new Regex("[0-9]+");
    
    public static List<Meeting2> ReadMeetings(GsRepository repo, string meetingsSheetName)
    {
        var meetingsDataRaw =
            SheetTableReader.ReadRowsFromSheet(repo, meetingsSheetName, 0, 0, Constants.FormattedMeetingsRowWidth);
        var positions = meetingsDataRaw[0].GetPositions();

        var meetings = new List<Meeting2>();
        foreach (var row in meetingsDataRaw.Skip(1))
            try
            {
                var disciplineType = GetDisciplineType(row[positions["DisciplineType"]]);
                var discipline = new Discipline(row[positions["Discipline"]], disciplineType);
                var meetingType = GetMeetingType(row[positions["MeetingType"]]);
                var teacher = new Teacher(row[positions["Teacher"]]);
                var groups = ParseGroups(row[positions["Groups"]]);
                var place = row[positions["Place"]];
                var roomSpecs = ParseRoomSpec(row[positions["RoomSpecs"]]);
                var duration = ParseInt(row[positions["Duration"]], 1);
                var weekTypeSpec = ParseWeekType(row[positions["WeekTypeSpec"]]);
                var meetingTimePriorities =
                    ParseMeetingTimePriorities(row[positions["MeetingTimePriorities"]]);
                var after = string.IsNullOrEmpty(row[positions["After"]])
                    ? (MeetingType?)null
                    : GetMeetingType(row[positions["After"]]);
                var priority = ParseInt(row[positions["Priority"]], int.MaxValue);
                var isFixed = Convert.ToBoolean(ParseInt(row[positions["IsFixed"]], 0));
                var ignore = Convert.ToBoolean(ParseInt(row[positions["Ignore"]], 0));

                var classRoom = string.IsNullOrEmpty(row[positions["ClassRoom"]]) ? null : row[positions["ClassRoom"]];
                var time = ParseMeetingTime(row[positions["Time"]]).FirstOrDefault((MeetingTime?)null);
                meetings.Add(new Meeting2(meetings.Count, discipline, meetingType, teacher, groups, place, roomSpecs,
                    duration, weekTypeSpec, meetingTimePriorities, after, priority, isFixed, ignore,
                    classRoom, time));
            }
            catch (Exception e)
            {
                throw new Exception(string.Join("|", row), e);
            }

        return meetings;
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
    
    private static DisciplineType GetDisciplineType(string rawDisciplineType)
    {
        return rawDisciplineType.ToLower() switch
        {
            "" => DisciplineType.Free,
            "with test" => DisciplineType.WithEntranceTest,
            "obligatory" => DisciplineType.Obligatory,
            _ => throw new FormatException($"Некорректный тип дисциплины: {rawDisciplineType}")
        };
    }

    public static List<Room> ReadRooms(GsRepository repo, string classroomsSheetName)
    {
        var roomDataRaw = SheetTableReader.ReadRowsFromSheet(repo, classroomsSheetName, 0, 0, 5);
        var classrooms = ParseClassrooms(roomDataRaw);
        return classrooms;
    }

    private static int ParseInt(string raw, int defaultValue)
    {
        return string.IsNullOrEmpty(raw) ? defaultValue : int.Parse(raw);
    }

    private static List<int> ParseGroups(string raw)
    {
        return raw.Replace(" ", "").Split(',').Select(int.Parse).Distinct().ToList();
    }

    private static RoomSpec[] ParseRoomSpec(string raw)
    {
        return string.IsNullOrWhiteSpace(raw)
            ? Array.Empty<RoomSpec>()
            : raw.Split(',')
                .Select(mgs => mgs.Trim())
                .Select(GetRoomSpec).ToArray();
    }

    private static List<Room> ParseClassrooms(List<List<string>> roomDataRaw)
    {
        var positions = new Dictionary<string, int>();
        for (int i = 0; i < roomDataRaw[0].Count; i++)
        {
            positions[roomDataRaw[0][i]] = i;
        }

        var rooms = new List<Room>();
        foreach (var row in roomDataRaw.Skip(1))
        {
            var roomName = row[positions["номер аудитории"]];
            var specs = new List<RoomSpec>();
            if (!string.IsNullOrWhiteSpace(row[positions["компьютерная"]]))
                specs.Add(RoomSpec.Computer);
            if (!string.IsNullOrWhiteSpace(row[positions["проектор"]]))
                specs.Add(RoomSpec.Projector);
            if (!string.IsNullOrWhiteSpace(row[positions["вместимость"]]))
                specs.Add(GetRoomSpec(row[positions["вместимость"]]));
            var lockedTimes = ParseRoomsTimeRequisitions(row[positions["Занятость"]]);
            rooms.Add(new Room(new Location("МатМех", roomName), specs, lockedTimes));
        }

        return rooms;
    }

    private static HashSet<MeetingTime> ParseRoomsTimeRequisitions(string rawMeetingTime)
    {
        return string.IsNullOrWhiteSpace(rawMeetingTime)
            ? new()
            : ParseMeetingTimes(rawMeetingTime).SelectMany(x => x).ToHashSet();
    }

    private static WeekType ParseWeekType(string? weekTypeRaw)
    {
        return string.IsNullOrEmpty(weekTypeRaw) ? WeekType.All : GetWeekType(weekTypeRaw);
    }

    public static List<List<MeetingTime>> ParseMeetingTimePriorities(string rawMeetingTime)
    {
        if (!string.IsNullOrWhiteSpace(rawMeetingTime)) return ParseMeetingTimes(rawMeetingTime);
        return new List<List<MeetingTime>>() { GetAllPossibleMeetingTimes(WeekType.All) };
    }

    public static List<MeetingTime> GetAllPossibleMeetingTimes(WeekType weekType)
    {
        var ans = new List<MeetingTime>();
        foreach (var dayOfWeek in (DayOfWeek[]) Enum.GetValues(typeof(DayOfWeek)))
        {
            if (dayOfWeek == DayOfWeek.Sunday) continue;
            
            for (var timeslot = 1; timeslot <= Constants.TimeSlots; timeslot++)
            {
                ans.Add(new MeetingTime(weekType, dayOfWeek, timeslot));
            }
        }

        return ans;
    }

    private static List<List<MeetingTime>> ParseMeetingTimes(string raw)
    {
        try
        {
            var meetingTimePriorities = new List<List<MeetingTime>>();

            var lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                meetingTimePriorities.Add(ParseMeetingTime(line).ToList());
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
        //line = line.Replace(" ", "");
        if (string.IsNullOrEmpty(line))
            yield break;

        var last = 0;    //0-none; 1-days; 2-hours; 3-weekType
        var days = new List<string>();
        var slots = new List<string>();
        var weekType = WeekType.All;
        foreach (var token in line.Split(new []{',', ' '}, StringSplitOptions.RemoveEmptyEntries))
        {
            var cur = DayOfWeekRegex.Match(token).Success ? 1 :
                NumberRegex.Match(token).Success ? 2 :
                WeekTypeRegex.Match(token).Success ? 3 :
                throw new FormatException();
            if (last == 0)
                last = cur;

            if (last == 2 && cur == 1 || last == 3 && cur is 1 or 2)
            {
                foreach (var day in GetDays(days))
                {
                    foreach (var slot in GetSlots(slots))
                    {
                        yield return new MeetingTime(weekType, day, slot);
                    }
                }
                days.Clear();
                slots.Clear();
                weekType = WeekType.All;
            }

            switch (cur) 
            {
                case 1:
                    days.Add(token);
                    break;
                case 2:
                    slots.Add(token);
                    break;
                case 3:
                    weekType = token == "чет" ? WeekType.Even : WeekType.Odd;
                    break;
                default:
                    throw new FormatException();
            }
            last = cur;
        }
        foreach (var day in GetDays(days))
        {
            foreach (var slot in GetSlots(slots))
            {
                yield return new MeetingTime(weekType, day, slot);
            }
        }
    }

    private static List<DayOfWeek> GetDays(IEnumerable<string> dayReqs)
    {
        var days = new List<DayOfWeek>();
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
    
    private static List<DayOfWeek> GetDays(string dayString) => GetDays(dayString.Split(','));

    private static List<int> GetSlots(string slotString) => GetSlots(slotString.Split(','));
    
    private static List<int> GetSlots(IEnumerable<string> slotReqs)
    {
        var slots = new List<int>();
        foreach (var req in slotReqs)
        {
            //пн-пт: 3,4 пара
            var tmp = req.Split('-');
            var firstSlot = int.Parse(tmp[0]);
            var lastSlot = tmp.Length == 1 ? firstSlot : int.Parse(tmp[1]);
            if (firstSlot < 1 || lastSlot > Constants.TimeSlots)
                throw new FormatException($"meeting slots must be between 1 and {Constants.TimeSlots}");
            for (var slot = firstSlot; slot <= lastSlot; slot++)
                slots.Add(slot);
        }
        return slots;
    }
}