using System.Collections.Generic;
using Infrastructure.GoogleSheetsRepository;
using NUnit.Framework;

namespace Testing.GoogleSheetsRepositoryTests
{
    //TODO надо бы зачищать таблицу между тестами
    [TestFixture]
    public class WriteReadTests
    {
        private const string Url =
            "https://docs.google.com/spreadsheets/d/1Q9imoj8xLFgp887NsYeW8ngJ53E5GHvKblrnfatEBHk/edit#gid=";

        private const string SheetName = "Testing";

        [Test]
        public void WriteRead()
        {
            const string credentialPath = "..\\..\\..\\..\\Credentials\\client_secrets.json";
            var repo = new GsRepository("test", credentialPath, Url);
            repo.ClearCellRange(SheetName, (0, 0), (10, 10));
            repo.SetUpSheetInfo();


            repo.ChangeTable(Url);
            repo.ReadCell(SheetName, (1, 1)); // var read =

            var dataToWrite = new List<List<string>>()
            {
                new() {"11", "12"},
                new() {"21", "22"},
                new() {"31", "32"}
            };
            repo.ModifySpreadSheet(SheetName)
                .WriteRange((1, 2), dataToWrite)
                .Execute();

            var valRange = repo.ReadCellRange(SheetName, (1, 2), (3, 4))!;
            for (var r = 0; r < valRange.Count; r++)
            for (var c = 0; c < valRange[r]!.Count; c++)
                Assert.AreEqual(dataToWrite[r][c], valRange[r]![c]);
        }

        [Test]
        public void WriteReadTwoReposWithSameCreds()
        {
            const string credentialPath = "..\\..\\..\\..\\Credentials\\client_secrets.json";
            var repo1 = new GsRepository("test", credentialPath, Url);
            repo1.ClearCellRange(SheetName, (0, 0), (10, 10));
            var repo2 = new GsRepository("test", credentialPath, Url);

            var dataToWrite = new List<List<string>>()
            {
                new() {"11", "12"},
                new() {"21", "22"},
                new() {"31", "32"}
            };
            repo1.ModifySpreadSheet(SheetName)
                .WriteRange((1, 2), dataToWrite)
                .Execute();

            var valRange = repo2.ReadCellRange(SheetName, (1, 2), (3, 4))!;
            for (var r = 0; r < valRange.Count; r++)
            for (var c = 0; c < valRange[r]!.Count; c++)
                Assert.AreEqual(dataToWrite[r][c], valRange[r]![c]);
        }
    }
}