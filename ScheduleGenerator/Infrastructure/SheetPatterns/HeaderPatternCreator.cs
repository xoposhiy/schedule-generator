using System.Collections.Generic;
using Google.Apis.Sheets.v4.Data;
using Infrastructure.GoogleSheetsRepository;

namespace Infrastructure.SheetPatterns
{
    public class HeaderPatternCreator
    {
        public static void SetUpHeaders(GsRepository repo, string sheetName,
            (int row, int col) start, List<string> headers, List<string> comments)
        {
            var modifier = repo
                .ModifySpreadSheet(sheetName);
            for (var i = 0; i < headers.Count; i++)
            {
                modifier
                    .WriteRange((start.row, start.col + i), new List<List<string>>() {new List<string>() {headers[i]}})
                    .AddBorders((start.row, start.col + i), (start.row, start.col + i), new Color() {Blue = 1});
                if (i < comments.Count) modifier.AddComment((start.row, start.col + i), comments[i]);
            }

            modifier.Execute();
        }
    }
}