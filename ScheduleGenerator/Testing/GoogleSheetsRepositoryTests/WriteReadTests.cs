using System.Collections.Generic;
using CommonInfrastructure.GoogleSheetsRepository;
using Google.Apis.Sheets.v4.Data;
using NUnit.Framework;
using static CommonInfrastructure.Extensions;
using static Infrastructure.SheetConstants;

namespace Testing.GoogleSheetsRepositoryTests
{
    [TestFixture]
    public class WriteReadTests
    {
        [SetUp]
        [TearDown]
        public void SetUp()
        {
            TestRepository.ClearCellRange(SheetName, 0, 0, 10, 10);
        }

        private readonly List<List<CellData>> dataToWrite = new()
        {
            new() {CommonCellData("11"), CommonCellData("12")},
            new() {CommonCellData("21"), CommonCellData("22")},
            new() {CommonCellData("31"), CommonCellData("32")}
        };

        [Test]
        public void WriteRead()
        {
            TestRepository.SetUpSheetInfo();
            TestRepository.ChangeTable(Url1);

            TestRepository.ModifySpreadSheet(SheetName)
                .WriteRange(1, 2, dataToWrite)
                .Execute();

            var valRange = TestRepository.ReadCellRange(SheetName, 1, 2, 3, 4)!;
            for (var r = 0; r < valRange.Count; r++)
            for (var c = 0; c < valRange[r]!.Count; c++)
                Assert.AreEqual(dataToWrite[r][c].UserEnteredValue.StringValue, valRange[r]![c]);
        }

        [Test]
        public void WriteReadTwoReposWithSameCredentials()
        {
            var repo2 = new GsRepository("test", CredentialPath, Url1);

            TestRepository.ModifySpreadSheet(SheetName)
                .WriteRange(1, 2, dataToWrite)
                .Execute();

            var valRange = repo2.ReadCellRange(SheetName, 1, 2, 3, 4)!;
            for (var r = 0; r < valRange.Count; r++)
            for (var c = 0; c < valRange[r]!.Count; c++)
                Assert.AreEqual(dataToWrite[r][c].UserEnteredValue.StringValue, valRange[r]![c]);
        }
    }
}