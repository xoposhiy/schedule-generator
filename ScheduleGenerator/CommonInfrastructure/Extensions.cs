using CommonInfrastructure.GoogleSheetsRepository;
using Google.Apis.Sheets.v4.Data;
using static CommonInfrastructure.Constants;

namespace CommonInfrastructure
{
    public static class Extensions
    {
        private static readonly DateTime Beginning = new(1899, 12, 30);
        public static readonly Color BackgroundColor = new() {Blue = 15 / 16f, Green = 15 / 16f, Red = 15 / 16f};
        public static readonly Color OnlineColor = new() {Blue = 1, Red = 15 / 16f, Green = 15 / 16f};

        public static CellData CommonCellData(string value)
        {
            return new()
            {
                UserEnteredValue = new()
                {
                    StringValue = value
                },
                UserEnteredFormat = new()
                {
                    TextFormat = new()
                    {
                        FontSize = 9
                    },
                    VerticalAlignment = "middle",
                    HorizontalAlignment = "center",
                    WrapStrategy = "wrap"
                }
            };
        }

        public static CellData HeaderCellData(string value)
        {
            var cellData = CommonCellData(value);
            cellData.UserEnteredFormat.TextFormat.Bold = true;
            return cellData;
        }

        public static CellData CommonBoolCellData(bool value = false)
        {
            return new()
            {
                UserEnteredValue = new()
                {
                    BoolValue = value
                },
                DataValidation = new()
                {
                    Condition = new()
                    {
                        Type = "BOOLEAN"
                    }
                }
            };
        }

        public static CellData CommonTimeCellData(DateTime dateTime)
        {
            return new()
            {
                UserEnteredFormat = new()
                {
                    NumberFormat = new()
                    {
                        Type = "DATE_TIME",
                        Pattern = "dd.mm.yyyy hh:mm:ss"
                    }
                },
                UserEnteredValue = new()
                {
                    NumberValue = (dateTime - Beginning).TotalDays
                }
            };
        }

        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            var batch = new List<T>();
            foreach (var e in source)
            {
                batch.Add(e);
                if (batch.Count < batchSize) continue;
                yield return batch;
                batch = new();
            }

            if (batch.Count > 0)
                yield return batch;
        }

        public static SheetModifier BuildTimeSlotsBar(this SheetModifier modifier,
            int startColumn, int rowStart,
            int height, int width,
            int count)
        {
            var classStarts = RomeNumbers
                .Take(count)
                .Select((n, i) => $"{n} {MeetingStartTimes[i]}")
                .ToList();

            foreach (var classStart in classStarts.Select(HeaderCellData))
            {
                modifier
                    .WriteRange(rowStart, startColumn, new() {new() {classStart}})
                    .AddBorders(rowStart, startColumn)
                    .MergeCell(rowStart, startColumn, height, width);
                rowStart += height;
            }

            return modifier;
        }
        
        public static string ToRuString(this DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "Понедельник",
                DayOfWeek.Tuesday => "Вторник",
                DayOfWeek.Wednesday => "Среда",
                DayOfWeek.Thursday => "Четверг",
                DayOfWeek.Friday => "Пятница",
                DayOfWeek.Saturday => "Суббота",
                DayOfWeek.Sunday => "Воскресенье",
                _ => throw new ArgumentOutOfRangeException(nameof(dayOfWeek), dayOfWeek, null)
            };
        }
    }
}