﻿using System;
using System.Collections.Generic;
using System.Linq;
using CommonInfrastructure.GoogleSheetsRepository;

namespace Infrastructure.SheetPatterns
{
    public static class SheetTableReader
    {
        public static List<List<string>> ReadRowsFromSheet(GsRepository repo, string sheetName, int top, int left,
            int width)
        {
            var sheetObj = repo.CurrentSheetInfo!.Spreadsheet.Sheets.First(s => s.Properties.Title == sheetName);
            var actualRowCount = sheetObj.Properties.GridProperties.RowCount!;
            var rowCountToRead = Math.Min((int) actualRowCount, 300);
            var sheetData = repo.ReadCellRange(sheetName, top, left, rowCountToRead, width);
            var res = new List<List<string>>();
            foreach (var row in sheetData!)
            {
                for (var i = width - row!.Count; i > 0; i--)
                    row.Add("");
                res.Add(row!);
            }

            return res;
        }
    }
}