using System;
using System.Collections.Generic;
using System.Linq;
using Infrastructure.GoogleSheetsRepository;

namespace Domain.Conversions
{
    public class ScheduleSpreadsheetConverter
    {
        private readonly GsRepository repository;
        private readonly string sheetName;
        private const int TimeBarRowOffset = 4;
        private const int TimeBarColumnOffset = 0;
        private const int HeadersColumnOffset = 2;
        private const int HeadersRowOffset = 2;
        private static readonly string[] WeekDays = {"ПН", "ВТ", "СР", "ЧТ", "ПТ", "СБ"};

        private static readonly string[] ClassStarts =
        {
            "I 9:00", "II 10:40", "III 12:50",
            "IV 14:30", "V 16:40", "VI 17:50"
        };

        public ScheduleSpreadsheetConverter(GsRepository repo, string sheetName)
        {
            repository = repo;
            this.sheetName = sheetName;
        }

        public void Build(IReadonlySchedule schedule)
        {
            var groupNamesSet = new HashSet<string>();
            var meetingSet = new HashSet<Meeting>();
            foreach (var meeting in schedule.GetMeetings())
            {
                foreach (var group in meeting.Groups!) groupNamesSet.Add(@group.GroupName);
                meetingSet.Add(meeting);
            }

            // foreach (var meeting in meetingSet)
            // {
            //     var data =
            //         $"{meeting.Discipline}, {meeting.Teacher?.Name}, {meeting.MeetingType},{string.Join(" ", meeting.Groups.ToList())}, {(int) meeting.MeetingTime.Day}, {meeting.MeetingTime.TimeSlotIndex}";
            //     Console.WriteLine(data);
            // }

            Console.WriteLine($"Прокинется дальше: {meetingSet.Count}, Было: {schedule.GetMeetings().ToList().Count}");
            var groupNames = groupNamesSet.OrderBy(gn => gn).ToList();

            PrepareSheet();

            BuildSchedulePattern(groupNames);

            FillScheduleData(meetingSet, groupNames);
        }

        private void PrepareSheet()
        {
            repository.ModifySpreadSheet(sheetName)
                .ClearAll()
                .UnMergeAll()
                .Execute();
        }

        private void BuildSchedulePattern(List<string> groups)
        {
            ColorField(groups);
            BuildTimeBar();
            BuildGroupHeaders(groups);
        }

        private void ColorField(List<string> groups)
        {
            var weekDayCount = WeekDays.Length;
            var startIndexesCount = ClassStarts.Length;
            var modifier = repository
                .ModifySpreadSheet(sheetName);
            modifier.ColorizeRange((TimeBarRowOffset, HeadersColumnOffset),
                (TimeBarRowOffset + weekDayCount * startIndexesCount * 2 - 1,
                    HeadersColumnOffset + groups.Count * 2 - 1),
                new() {Blue = 15 / 16f, Green = 15 / 16f, Red = 15 / 16f, Alpha = 1 - 0.05f});

            modifier.Execute();
        }

        private void BuildTimeBar()
        {
            var weekDayCount = WeekDays.Length;
            var startIndexesCount = ClassStarts.Length;
            var modifier = repository
                .ModifySpreadSheet(sheetName);
            var currentStart = TimeBarRowOffset;
            for (var i = 0; i < weekDayCount; i++)
            {
                modifier
                    .WriteRange((currentStart, TimeBarColumnOffset), new() {new() {WeekDays[i]}})
                    .AddBorders((currentStart, TimeBarColumnOffset), (currentStart + 11, TimeBarColumnOffset))
                    .MergeCell((currentStart, TimeBarColumnOffset), (currentStart + 11, TimeBarColumnOffset));
                currentStart += 12;
            }

            currentStart = TimeBarRowOffset;
            for (var i = 0; i < weekDayCount * startIndexesCount; i++)
            {
                modifier
                    .WriteRange((currentStart, TimeBarColumnOffset + 1),
                        new() {new() {ClassStarts[i % 6]}})
                    .AddBorders((currentStart, TimeBarColumnOffset + 1), (currentStart + 1, TimeBarColumnOffset + 1))
                    .MergeCell((currentStart, TimeBarColumnOffset + 1), (currentStart + 1, TimeBarColumnOffset + 1));
                currentStart += 2;
            }

            modifier.Execute();
        }

        private void BuildGroupHeaders(List<string> groups)
        {
            var modifier = repository
                .ModifySpreadSheet(sheetName);
            var currentStart = HeadersColumnOffset;
            for (var i = 0; i < groups.Count; i++)
            {
                modifier
                    .WriteRange((HeadersRowOffset, currentStart), new() {new() {groups[i]}})
                    .AddBorders((HeadersRowOffset, currentStart), (HeadersRowOffset, currentStart + 1))
                    .MergeCell((HeadersRowOffset, currentStart), (HeadersRowOffset, currentStart + 1))
                    .WriteRange((HeadersRowOffset + 1, currentStart),
                        new() {new() {groups[i] + "-1", groups[i] + "-2"}})
                    .AddBorders((HeadersRowOffset + 1, currentStart), (HeadersRowOffset + 1, currentStart))
                    .AddBorders((HeadersRowOffset + 1, currentStart + 1), (HeadersRowOffset + 1, currentStart + 1));
                currentStart += 2;
            }

            modifier.Execute();
        }

        private void FillScheduleData(HashSet<Meeting> meetings, List<string> groups)
        {
            var groupIndexDict = groups
                .Select((g, i) => (g, i))
                .ToDictionary(gi => gi.g, gi => gi.i);
            var modifier = repository
                .ModifySpreadSheet(sheetName);

            foreach (var meeting in meetings) WriteMeeting(meeting, groupIndexDict, modifier);
            modifier.Execute();
        }

        private void WriteMeeting(Meeting meeting, Dictionary<string, int> groupIndexDict, SheetModifier modifier)
        {
            var horizOffset = 2;
            var vertOffset = 2;

            var weekDayToIntDict = new Dictionary<DayOfWeek, int>()
            {
                {DayOfWeek.Monday, 0},
                {DayOfWeek.Tuesday, 1},
                {DayOfWeek.Wednesday, 2},
                {DayOfWeek.Thursday, 3},
                {DayOfWeek.Friday, 4},
                {DayOfWeek.Saturday, 5}
                // { DayOfWeek.Sunday, 6}
            };


            foreach (var group in meeting.Groups!)
            {
                // var data = $"{meeting.Discipline}, {meeting.Teacher?.Name}, {meeting.MeetingTime}";
                var data =
                    $"{meeting.Discipline}, " +
                    $"{meeting.Location}, " +
                    $"{meeting.Teacher?.Name}";

                var rowNumOff = weekDayToIntDict[meeting.MeetingTime!.Day] * 12 + vertOffset;
                var rowNum = meeting.MeetingTime.TimeSlotIndex * 2 + rowNumOff;
                var rowsInMeeting = 1;
                if (meeting.WeekType == WeekType.Even) rowNum++;
                if (meeting.WeekType == WeekType.All) rowsInMeeting = 2;

                var colNum = groupIndexDict[group.GroupName] * 2 + horizOffset;
                var columnsInMeeting = 1;
                if (group.GroupPart == GroupPart.Part2) colNum++;
                if (group.GroupPart == GroupPart.FullGroup) columnsInMeeting = 2;
                modifier
                    .WriteRange((rowNum, colNum), new() {new() {data}})
                    .AddBorders((rowNum, colNum), (rowNum + rowsInMeeting - 1, colNum + columnsInMeeting - 1));
                if (rowsInMeeting == 2 || columnsInMeeting == 2)
                    modifier.MergeCell((rowNum, colNum), (rowNum + rowsInMeeting - 1, colNum + columnsInMeeting - 1));
            }
        }
    }
}