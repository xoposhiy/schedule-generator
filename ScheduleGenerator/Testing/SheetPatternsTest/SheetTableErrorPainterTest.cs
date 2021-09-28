using System;
using System.Collections.Generic;
using System.Linq;
using Infrastructure.GoogleSheetsRepository;
using Infrastructure.SheetPatterns;
using NUnit.Framework;

namespace Testing.SheetPatternsTest
{
    internal class SheetTableErrorPainterTest
    {
        private static string CredentialsEnvVar = "GoogleApiCredentials";
        private static readonly string ApplicationName = "MyApp";

        private static string url =
            "https://docs.google.com/spreadsheets/d/1JxL_CTuc-NLaBRdY5F4vz3yn6WJe8bp-7Mn7ViycjtQ/edit#gid=566045364";

        private static string sheetName = "SheetTablePainterPractice";

        [Test]
        public void PaintErrorsWhenNoMergerCells()
        {
            var credentialDirPath = Environment.GetEnvironmentVariable(CredentialsEnvVar);
            var credentialPath = credentialDirPath + "\\client_secrets.json";
            var repo = new GsRepository(ApplicationName, credentialPath, url);


            var errors = new List<((int, int), string)>()
            {
                ((0, 0), "error msg1"), ((1, 1), "error msg2"), ((4, 2), "error msg3")
            };

            SheetTableErrorPainter.PaintErrors(repo, sheetName, (1, 0), errors);
            Assert.Pass();
        }

        [Test]
        public void PaintErrorsAndClear()
        {
            var credentialDirPath = Environment.GetEnvironmentVariable(CredentialsEnvVar);
            var credentialPath = credentialDirPath + "\\client_secrets.json";
            var repo = new GsRepository(ApplicationName, credentialPath, url);

            var errors = new List<((int, int), string)>()
            {
                ((0, 0), "error msg1"), ((1, 1), "error msg2"), ((4, 2), "error msg3")
            };
            SheetTableErrorPainter.PaintErrors(repo, sheetName, (5, 0), errors);

            var coordsToClear = errors.Select(e => e.Item1);
            SheetTableErrorPainter.ClearErrorPaint(repo, sheetName, (5, 0), coordsToClear);

            Assert.Pass();
        }
    }
}