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
                    BackgroundColor = new() {Blue = 1, Green = 1, Red = 1},
                    VerticalAlignment = "middle",
                    HorizontalAlignment = "center",
                    WrapStrategy = "wrap"
                }
            };
        }
    }
}