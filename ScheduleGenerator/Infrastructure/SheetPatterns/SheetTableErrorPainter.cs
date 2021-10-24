using System.Collections.Generic;
using Infrastructure.GoogleSheetsRepository;

namespace Infrastructure.SheetPatterns
{
    public static class SheetTableErrorPainter
    {
        public static void PaintErrors(GsRepository repo, string sheetName,
            (int row, int col) offset, List<((int row, int col) coords, string msg)> errors)
        {
            var modifier = repo.ModifySpreadSheet(sheetName);
            foreach (var (coords, msg) in errors)
            {
                var cellToModify = (coords.row + offset.row, coords.col + offset.col);
                modifier.AddComment(cellToModify, msg)
                    .ColorizeRange(coords.row + offset.row, coords.col + offset.col, 1, 1, new() {Red = 1});
            }

            modifier.Execute();
        }

        public static void ClearErrorPaint(GsRepository repo, string sheetName, (int row, int col) offset,
            IEnumerable<(int row, int col)> coords)
        {
            var modifier = repo.ModifySpreadSheet(sheetName);
            foreach (var coord in coords)
            {
                var cellToModify = (coord.row + offset.row, coord.col + offset.col);
                modifier.AddComment(cellToModify, null);
                modifier.ColorizeRange(coord.row + offset.row, coord.col + offset.col, 1, 1,
                    new() {Red = 1, Green = 1, Blue = 1});
            }

            modifier.Execute();
        }
    }
}