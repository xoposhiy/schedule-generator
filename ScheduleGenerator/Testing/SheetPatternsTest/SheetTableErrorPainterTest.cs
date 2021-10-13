using System.Collections.Generic;
using System.Linq;
using Infrastructure.GoogleSheetsRepository;
using Infrastructure.SheetPatterns;
using NUnit.Framework;

namespace Testing.SheetPatternsTest
{
    [TestFixture]
    internal class SheetTableErrorPainterTest
    {
        private const string Url =
            "https://docs.google.com/spreadsheets/d/1Q9imoj8xLFgp887NsYeW8ngJ53E5GHvKblrnfatEBHk/edit#gid=";

        private static string SheetName = "Testing";

        [Test]
        public void PaintErrorsWhenNoMergerCells()
        {
            const string credentialPath = "..\\..\\..\\..\\Credentials\\client_secrets.json";
            var repo = new GsRepository("test", credentialPath, Url);
            repo.ClearCellRange(SheetName, (0, 0), (10, 10));


            var errors = new List<((int, int), string)>()
            {
                ((0, 0), "error msg1"), ((1, 1), "error msg2"), ((4, 2), "error msg3")
            };

            SheetTableErrorPainter.PaintErrors(repo, SheetName, (1, 0), errors);
            Assert.Pass();
            //TODO: test should probably check something
        }

        [Test]
        public void PaintErrorsAndClear()
        {
            const string credentialPath = "..\\..\\..\\..\\Credentials\\client_secrets.json";
            var repo = new GsRepository("test", credentialPath, Url);
            repo.ClearCellRange(SheetName, (0, 0), (10, 10));

            var errors = new List<((int, int), string)>()
            {
                ((0, 0), "error msg1"), ((1, 1), "error msg2"), ((4, 2), "error msg3")
            };
            SheetTableErrorPainter.PaintErrors(repo, SheetName, (5, 0), errors);

            var coordsToClear = errors.Select(e => e.Item1);
            SheetTableErrorPainter.ClearErrorPaint(repo, SheetName, (5, 0), coordsToClear);

            Assert.Pass();
            //TODO: test should probably check something
        }
    }
}