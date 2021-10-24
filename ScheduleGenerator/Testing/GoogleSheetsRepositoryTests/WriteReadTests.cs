using System.Collections.Generic;
using Google.Apis.Sheets.v4.Data;
using Infrastructure.GoogleSheetsRepository;
using NUnit.Framework;
using static Infrastructure.Extensions;

namespace Testing.GoogleSheetsRepositoryTests
{
    [TestFixture]
    public class WriteReadTests
    {
        private const string Url =
            "https://docs.google.com/spreadsheets/d/1Q9imoj8xLFgp887NsYeW8ngJ53E5GHvKblrnfatEBHk/edit#gid=";

        private const string SheetName = "Testing";
        private const string CredentialPath = "..\\..\\..\\..\\Credentials\\client_secrets.json";

        private readonly List<List<CellData>> dataToWrite = new()
        {
            new() {CommonCellData("11"), CommonCellData("12")},
            new() {CommonCellData("21"), CommonCellData("22")},
            new() {CommonCellData("31"), CommonCellData("32")}
        };

        private readonly GsRepository repository = new("test", CredentialPath, Url);

        [SetUp]
        [TearDown]
        public void SetUp()
        {
            repository.ClearCellRange(SheetName, (0, 0), (10, 10));
        }

        [Test]
        public void WriteRead()
        {
            repository.SetUpSheetInfo();
            repository.ChangeTable(Url);

            repository.ModifySpreadSheet(SheetName)
                .WriteRange(1, 2, dataToWrite)
                .Execute();

            var valRange = repository.ReadCellRange(SheetName, (1, 2), (3, 4))!;
            for (var r = 0; r < valRange.Count; r++)
            for (var c = 0; c < valRange[r]!.Count; c++)
                Assert.AreEqual(dataToWrite[r][c].UserEnteredValue.StringValue, valRange[r]![c]);
        }

        [Test]
        public void WriteReadTwoReposWithSameCredentials()
        {
            var repo2 = new GsRepository("test", CredentialPath, Url);

            repository.ModifySpreadSheet(SheetName)
                .WriteRange(1, 2, dataToWrite)
                .Execute();

            var valRange = repo2.ReadCellRange(SheetName, (1, 2), (3, 4))!;
            for (var r = 0; r < valRange.Count; r++)
            for (var c = 0; c < valRange[r]!.Count; c++)
                Assert.AreEqual(dataToWrite[r][c].UserEnteredValue.StringValue, valRange[r]![c]);
        }
    }
}