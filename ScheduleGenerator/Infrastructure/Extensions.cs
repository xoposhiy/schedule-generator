﻿using Google.Apis.Sheets.v4.Data;

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
    }
}