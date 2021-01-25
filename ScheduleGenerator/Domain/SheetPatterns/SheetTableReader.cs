using System;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Sheets.v4.Data;

using Domain.GoogleSheetsRepository;

namespace Domain.SheetPatterns
{
    public class SheetTableReader
    {

        public static List<List<string>> ReadRowsFromSheet(GSRepository repo, string SheetName, (int row, int col) start, int width)
        {
            var sheetObj = repo.CurrentSheetInfo.spreadsheet.Sheets.Where(s => s.Properties.Title == SheetName).First();
            var actualRowCount = sheetObj.Properties.GridProperties.RowCount;
            var rowCountToRead = Math.Min((int)actualRowCount, 300);
            var sheetData = repo.ReadCellRange(SheetName, start, (rowCountToRead, width));
            foreach (var row in sheetData)
            {
                for (var i = width - row.Count; i > 0; i--)
                {
                    row.Add("");
                }
            }
            return sheetData;
        }
    }
}
