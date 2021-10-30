using System.Collections.Generic;
using Google.Apis.Sheets.v4.Data;
using Infrastructure.GoogleSheetsRepository;
using NUnit.Framework;
using static Infrastructure.Extensions;
using static Infrastructure.SheetConstants;

namespace Testing.GoogleSheetsRepositoryTests
{
    [TestFixture]
    public class WriteReadTests
    {
        private readonly List<List<CellData>> dataToWrite = new()
        {
            new() {CommonCellData("11"), CommonCellData("12")},
            new() {CommonCellData("21"), CommonCellData("22")},
            new() {CommonCellData("31"), CommonCellData("32")}
        };

        [SetUp]
        [TearDown]
        public void SetUp()
        {
            Repository.ClearCellRange(SheetName, 0, 0, 10, 10);
        }

        [Test]
        public void WriteRead()
        {
            Repository.SetUpSheetInfo();
            Repository.ChangeTable(Url);

            Repository.ModifySpreadSheet(SheetName)
                .WriteRange(1, 2, dataToWrite)
                .Execute();

            var valRange = Repository.ReadCellRange(SheetName, 1, 2, 3, 4)!;
            for (var r = 0; r < valRange.Count; r++)
            for (var c = 0; c < valRange[r]!.Count; c++)
                Assert.AreEqual(dataToWrite[r][c].UserEnteredValue.StringValue, valRange[r]![c]);
        }

        [Test]
        public void WriteReadTwoReposWithSameCredentials()
        {
            var repo2 = new GsRepository("test", CredentialPath, Url);

            Repository.ModifySpreadSheet(SheetName)
                .WriteRange(1, 2, dataToWrite)
                .Execute();

            var valRange = repo2.ReadCellRange(SheetName, 1, 2, 3, 4)!;
            for (var r = 0; r < valRange.Count; r++)
            for (var c = 0; c < valRange[r]!.Count; c++)
                Assert.AreEqual(dataToWrite[r][c].UserEnteredValue.StringValue, valRange[r]![c]);
        }
    }
}