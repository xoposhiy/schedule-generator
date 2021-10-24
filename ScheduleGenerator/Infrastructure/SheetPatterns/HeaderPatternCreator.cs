using System.Collections.Generic;
using Infrastructure.GoogleSheetsRepository;
using static Infrastructure.Extensions;

namespace Infrastructure.SheetPatterns
{
    public static class HeaderPatternCreator
    {
        public static void SetUpHeaders(GsRepository repo, string sheetName, int startRow, int startColumn,
            List<string> headers, List<string> comments)
        {
            var modifier = repo
                .ModifySpreadSheet(sheetName);
            for (var i = 0; i < headers.Count; i++)
            {
                modifier
                    .WriteRange(startRow, startColumn + i, new() {new() {CommonCellData(headers[i])}})
                    .AddBorders(startRow, startColumn + i);
                if (i < comments.Count) modifier.AddComment(startRow, startColumn + i, comments[i]);
            }

            modifier.Execute();
        }
    }
}