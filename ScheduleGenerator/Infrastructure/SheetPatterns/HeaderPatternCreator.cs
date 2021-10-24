using System.Collections.Generic;
using Infrastructure.GoogleSheetsRepository;
using static Infrastructure.Extensions;

namespace Infrastructure.SheetPatterns
{
    public static class HeaderPatternCreator
    {
        public static void SetUpHeaders(GsRepository repo, string sheetName,
            (int row, int col) start, List<string> headers, List<string> comments)
        {
            var modifier = repo
                .ModifySpreadSheet(sheetName);
            for (var i = 0; i < headers.Count; i++)
            {
                modifier
                    .WriteRange((start.row, start.col + i), new() {new() {CommonCellData(headers[i])}})
                    .AddBorders((start.row, start.col + i), (start.row, start.col + i));
                if (i < comments.Count) modifier.AddComment((start.row, start.col + i), comments[i]);
            }

            modifier.Execute();
        }
    }
}