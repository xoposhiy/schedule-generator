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
            foreach (var (coord, msg) in errors)
            {
                var top = coord.row + offset.row;
                var left = coord.col + offset.col;
                modifier
                    .AddNote(top, left, msg)
                    .ColorizeRange(top, left, 1, 1, new() {Red = 1});
            }

            modifier.Execute();
        }

        public static void ClearErrorPaint(GsRepository repo, string sheetName, (int row, int col) offset,
            IEnumerable<(int row, int col)> coords)
        {
            var modifier = repo.ModifySpreadSheet(sheetName);
            foreach (var coord in coords)
            {
                var top = coord.row + offset.row;
                var left = coord.col + offset.col;
                modifier
                    .AddNote(top, left, null)
                    .ColorizeRange(top, left, 1, 1, new() {Red = 1, Green = 1, Blue = 1});
            }

            modifier.Execute();
        }
    }
}