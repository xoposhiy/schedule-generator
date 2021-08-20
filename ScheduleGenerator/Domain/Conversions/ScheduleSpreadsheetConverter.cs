using System;
using System.Collections.Generic;
using System.Linq;
using Infrastructure.GoogleSheetsRepository;
using Domain.ScheduleLib;
using Google.Apis.Sheets.v4.Data;

namespace Domain.Conversions
{
    public class ScheduleSpreadsheetConverter
    {
        private GSRepository repository;
        private string sheetName;
        private int timeBarRowOffset = 4;
        private int timeBarColumnOffset = 0;
        private int headersColumnOffset = 2;
        private int headersRowOffset = 2;
        private Dictionary<string, string> debugDict = new ();
        
        public ScheduleSpreadsheetConverter(GSRepository repo, string sheetName)
        {
            repository = repo;
            this.sheetName = sheetName;
        }

        public void Build(Schedule schedule)
        {
            var groupNamesSet = new HashSet<string>();
            var meetingSet = new HashSet<Meeting>();
            foreach (var meeting in schedule.Meetings)
            {
                foreach (var group in meeting.Groups)
                {
                    groupNamesSet.Add(group.GroupName);
                }
                meetingSet.Add(meeting);
            }

            foreach (var meeting in meetingSet)
            {
                var data =
                    $"{meeting.Discipline}, {meeting.Teacher?.Name}, {meeting.MeetingType},{string.Join(" ", meeting.Groups.ToList())}, {(int) meeting.MeetingTime.Day}, {meeting.MeetingTime.TimeSlotIndex}";
                Console.WriteLine(data);
            }

            Console.WriteLine($"Прокинется дальше: {meetingSet.Count}, Было: {schedule.Meetings.Count}");
            var groupNames = groupNamesSet.OrderBy(gn => gn).ToList();

            PrepareSheet();

            BuildSchedulePattern(groupNames);
            
            FillScheduleData(meetingSet, groupNames);
        }

        private void PrepareSheet() {
            repository.ModifySpreadSheet(sheetName)
                .ClearAll()
                .UnMergeAll()
                .Execute();
        }

        private void BuildSchedulePattern(List<string> groups)
        {
            BuildTimeBar();
            BuildGroupHeaders(groups);
        }

        private void BuildTimeBar()
        {
            var weekDays = new[] { "ПН", "ВТ", "СР", "ЧТ", "ПТ", "СБ" };
            var classStarts = new[] { "I 9:00", "II 10:40", "III 12:50",
                "IV 14:30", "V 16:40", "VI 17:50"};
            var weekDayCount = weekDays.Length;
            var startIndexesCount = classStarts.Length;
            var modifier = repository
                    .ModifySpreadSheet(sheetName);
            var currentStart = timeBarRowOffset;
            for (int i = 0; i < weekDayCount; i++)
            {
                modifier
                    .WriteRange((currentStart, timeBarColumnOffset), new List<List<string>>() { new List<string>() { weekDays[i] } })
                    .AddBorders((currentStart, timeBarColumnOffset), (currentStart + 11, timeBarColumnOffset), new Color() { Blue = 1 })
                    .MergeCell((currentStart, timeBarColumnOffset), (currentStart + 11, timeBarColumnOffset));
                currentStart += 12;
            }
            currentStart = timeBarRowOffset;
            for (int i = 0; i < weekDayCount * startIndexesCount; i++)
            {
                modifier
                    .WriteRange((currentStart, timeBarColumnOffset + 1), new List<List<string>> { new() { classStarts[i % 6] } })
                    .AddBorders((currentStart, timeBarColumnOffset + 1), (currentStart + 1, timeBarColumnOffset + 1), new Color() { Blue = 1 })
                    .MergeCell((currentStart, timeBarColumnOffset + 1), (currentStart + 1, timeBarColumnOffset + 1));
                currentStart += 2;
            }
            modifier.Execute();
        }

        private void BuildGroupHeaders(List<string> groups)
        {
            var modifier = repository
                    .ModifySpreadSheet(sheetName);
            var currentStart = headersColumnOffset;
            for (int i = 0; i < groups.Count; i++)
            {
                modifier
                    .WriteRange((headersRowOffset, currentStart), new List<List<string>> {new() {groups[i]}})
                    .AddBorders((headersRowOffset, currentStart), (headersRowOffset, currentStart + 1),
                        new Color() {Blue = 1})
                    .MergeCell((headersRowOffset, currentStart), (headersRowOffset, currentStart + 1))
                    .WriteRange((headersRowOffset+1, currentStart),
                        new List<List<string>> {new() {groups[i] + "-1", groups[i] + "-2"}})
                    .AddBorders((headersRowOffset+1, currentStart), (headersRowOffset+1, currentStart),
                        new Color {Blue = 1})
                    .AddBorders((headersRowOffset+1, currentStart + 1), (headersRowOffset+1, currentStart + 1),
                        new Color {Blue = 1});
                currentStart += 2;
            }
            modifier.Execute();
        }

        private void FillScheduleData(HashSet<Meeting> meetings, List<string> groups)
        {
            var GroupIndexDict = groups
                .Select((g, i) => (g, i))
                .ToDictionary(gi => gi.g, gi => gi.i);
            var modifier = repository
                    .ModifySpreadSheet(sheetName);

            foreach (var meeting in meetings)
            {
                WriteMeeting(meeting, GroupIndexDict, modifier);
            }
            modifier.Execute();
        }

        private void WriteMeeting(Meeting meeting, Dictionary<string, int> groupIndexDict, SheetModifier modifier)
        {
            var horizOffset = 2;
            var vertOffset = 2;

            var weekDayToIntDict = new Dictionary<DayOfWeek, int>() {
                { DayOfWeek.Monday, 0 },
                { DayOfWeek.Tuesday, 1 },
                { DayOfWeek.Wednesday, 2 },
                { DayOfWeek.Thursday, 3 },
                { DayOfWeek.Friday, 4 },
                { DayOfWeek.Saturday, 5 },
                // { DayOfWeek.Sunday, 6}
            };
            

            foreach (var group in meeting.Groups)
            {
                // var data = $"{meeting.Discipline}, {meeting.Teacher?.Name}, {meeting.MeetingTime}";
                var data = $"{meeting.Discipline}, {meeting.Teacher?.Name}, {meeting.Location}, {meeting.MeetingType}, {group}, {(int)meeting.MeetingTime.Day}, {meeting.MeetingTime.TimeSlotIndex}";
                // Console.WriteLine(data);
                var key = $"{group.GroupName}, {(int) meeting.MeetingTime.Day}, {meeting.MeetingTime.TimeSlotIndex}";
                if (!debugDict.TryAdd(key, data))
                {
                    Console.WriteLine($"Warning: {debugDict[key]},\n {data}");
                }
                
                var rowNumOff = weekDayToIntDict[meeting.MeetingTime.Day] * 12 + vertOffset;
                var rowNum = meeting.MeetingTime.TimeSlotIndex * 2 + rowNumOff;
                var rowsInMeeting = 1;
                if (meeting.WeekType == WeekType.Even)
                {
                    rowNum++;
                }
                if (meeting.WeekType == WeekType.All)
                {
                    rowsInMeeting = 2;
                }

                var colNum = groupIndexDict[group.GroupName] * 2 + horizOffset;
                var columnsInMeeting = 1;
                if (group.GroupPart == GroupPart.Part2)
                {
                    colNum++;
                }
                if (group.GroupPart == GroupPart.FullGroup)
                {
                    columnsInMeeting = 2;
                }
                // Console.WriteLine($"{meeting}");
                // Console.WriteLine($"rowNumOff: {rowNumOff}");
                // Console.WriteLine($"C: {colNum} R:{rowNum} C: {colNum + columnsInMeeting - 1} R: {rowNum + rowsInMeeting - 1}");
                
                // Console.WriteLine($"{rowNum}, {colNum}");
                // if (!debugDict.TryAdd($"{rowNum}, {colNum}", meeting))
                //     Console.WriteLine($"{meeting}, {debugDict[$"{rowNum}, {colNum}"]}");
                modifier
                    .WriteRange((rowNum, colNum), new List<List<string>>() { new List<string>() { data } })
                    .AddBorders((rowNum, colNum), (rowNum + rowsInMeeting - 1, colNum + columnsInMeeting - 1), new Color() { Green = 1 });
                if (rowsInMeeting == 2 || columnsInMeeting == 2)
                {
                    // Console.WriteLine($"{(rowNum, colNum)}");
                    // Console.WriteLine($"{(rowNum + rowsInMeeting - 1, colNum + columnsInMeeting - 1)}");
                    modifier.MergeCell((rowNum, colNum), (rowNum + rowsInMeeting - 1, colNum + columnsInMeeting - 1));
                    // Console.WriteLine($"{rowNum}, {colNum+1}");
                    // if (!debugDict.TryAdd($"{rowNum}, {colNum + 1}", meeting))
                    //     Console.WriteLine($"{meeting}, {debugDict[$"{rowNum}, {colNum + 1}"]}");
                }
            }

        }
    }
}
