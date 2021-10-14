using System.Collections.Generic;
using Infrastructure.GoogleSheetsRepository;

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
                    .WriteRange((start.row, start.col + i), new List<List<string>> {new List<string> {headers[i]}})
                    .AddBorders((start.row, start.col + i), (start.row, start.col + i));
                if (i < comments.Count) modifier.AddComment((start.row, start.col + i), comments[i]);
            }

            modifier.Execute();
        }
    }
}