using System;
using Google.Apis.Sheets.v4.Data;

namespace Infrastructure
{
    public static class Extensions
    {
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

        private static readonly DateTime Beginning = new(1899, 12, 30);

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
    }
}