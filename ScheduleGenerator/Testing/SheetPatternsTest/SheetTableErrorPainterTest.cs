using System.Collections.Generic;
using System.Linq;
using Infrastructure.GoogleSheetsRepository;
using NUnit.Framework;
using static Infrastructure.SheetPatterns.SheetTableErrorPainter;

namespace Testing.SheetPatternsTest
{
    [TestFixture]
    internal class SheetTableErrorPainterTest
    {
        private const string Url =
            "https://docs.google.com/spreadsheets/d/1Q9imoj8xLFgp887NsYeW8ngJ53E5GHvKblrnfatEBHk/edit#gid=";

        private const string CredentialPath = "..\\..\\..\\..\\Credentials\\client_secrets.json";

        private const string SheetName = "Testing";

        private readonly List<((int, int), string)> errors = new()
        {
            ((0, 0), "error msg1"), ((1, 1), "error msg2"), ((4, 2), "error msg3")
        };

        [Test]
        public void PaintErrorsWhenNoMergerCells()
        {
            var repo = new GsRepository("test", CredentialPath, Url);
            repo.ClearCellRange(SheetName, (0, 0), (10, 10));

            PaintErrors(repo, SheetName, (1, 0), errors);
            var coordsToClear = errors.Select(e => e.Item1);
            ClearErrorPaint(repo, SheetName, (1, 0), coordsToClear);
            Assert.Pass();
            //TODO: test should probably check something
        }

        [Test]
        public void PaintErrorsAndClear()
        {
            var repo = new GsRepository("test", CredentialPath, Url);
            repo.ClearCellRange(SheetName, (0, 0), (10, 10));

            PaintErrors(repo, SheetName, (5, 0), errors);

            var coordsToClear = errors.Select(e => e.Item1);
            ClearErrorPaint(repo, SheetName, (5, 0), coordsToClear);

            Assert.Pass();
            //TODO: test should probably check something
        }
    }
}