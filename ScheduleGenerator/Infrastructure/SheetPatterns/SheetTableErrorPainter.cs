using System.Collections.Generic;
using Google.Apis.Sheets.v4.Data;
using Infrastructure.GoogleSheetsRepository;

namespace Infrastructure.SheetPatterns
{
    public class SheetTableErrorPainter
    {
        public static void PaintErrors(GsRepository repo, string sheetName,
                (int row, int col) offset, List<((int row, int col) coords, string msg)> errors)
        {
            var modifier = repo.ModifySpreadSheet(sheetName);
            foreach (var error in errors)
            {
                var cellToModify = (error.coords.row + offset.row, error.coords.col + offset.col);
                modifier.AddComment(cellToModify, error.msg);
                modifier.ColorizeRange(cellToModify, cellToModify, new Color { Red = 1 });
            }

            modifier.Execute();
        }

        public static void ClearErrorPaint(GsRepository repo, string sheetName, (int row, int col) offset, IEnumerable<(int row, int col)> coords)
        {
            var modifier = repo.ModifySpreadSheet(sheetName);
            foreach (var coord in coords)
            {
                var cellToModify = (coord.row + offset.row, coord.col + offset.col);
                modifier.AddComment(cellToModify, null);
                modifier.ColorizeRange(cellToModify, cellToModify, new Color { Red = 1, Green = 1, Blue = 1 });
            }

            modifier.Execute();
        }
    }
}
