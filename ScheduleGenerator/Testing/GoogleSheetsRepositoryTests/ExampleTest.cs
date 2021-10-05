using System;
using System.Collections.Generic;
using System.Linq;
using Infrastructure.GoogleSheetsRepository;
using NUnit.Framework;

namespace Testing.GoogleSheetsRepositoryTests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        // [Test]
        public void Test1()
        {
            var ApplicationName = "MyApp";
            var credentialDirPath = Environment.GetEnvironmentVariable("GoogleApiCredentials");
            var credentialPath = credentialDirPath + "\\client_secrets.json";
            var url = "https://docs.google.com/spreadsheets/d/1ncJ77JJJapPJpTeDpvWIsmuavStz5aXwQLs7fc89Rgo/edit#gid=0";
            var repo = new GsRepository(ApplicationName, credentialPath, url);

            repo.ChangeTable(url);
            repo.ReadCell(repo.CurrentSheetInfo!.Sheets.Keys.First(), (1, 1)); // var read =

            var dataToWrite = new List<List<string>>()
            {
                new() {"11", "12"},
                new() {"21", "22"},
                new() {"31", "32"}
            };
            repo.ModifySpreadSheet(repo.CurrentSheetInfo.Sheets.Keys.First())
                .WriteRange((1, 2), dataToWrite)
                .Execute();

            var valRange = repo.ReadCellRange(repo.CurrentSheetInfo.Sheets.Keys.First(), (1, 2), (3, 4))!;
            for (var r = 0; r < valRange.Count; r++)
            for (var c = 0; c < valRange[r]!.Count; c++)
                Assert.AreEqual(dataToWrite[r][c], valRange[r]![c]);
        }

        [Test]
        public void TwoReposWithSameCreds()
        {
            var ApplicationName = "MyApp";
            var credentialDirPath = Environment.GetEnvironmentVariable("GoogleApiCredentials");
            var credentialPath = credentialDirPath + "\\client_secrets.json";
            var url = "https://docs.google.com/spreadsheets/d/1ncJ77JJJapPJpTeDpvWIsmuavStz5aXwQLs7fc89Rgo/edit#gid=0";
            var repo1 = new GsRepository(ApplicationName, credentialPath, url);
            var repo2 = new GsRepository(ApplicationName, credentialPath, url);

            var dataToWrite = new List<List<string>>()
            {
                new() {"11", "12"},
                new() {"21", "22"},
                new() {"31", "32"}
            };
            repo1.ModifySpreadSheet(repo1.CurrentSheetInfo!.Sheets.Keys.First())
                .WriteRange((1, 2), dataToWrite)
                .Execute();

            var valRange = repo2.ReadCellRange(repo2.CurrentSheetInfo!.Sheets.Keys.First(), (1, 2), (3, 4))!;
            for (var r = 0; r < valRange.Count; r++)
            for (var c = 0; c < valRange[r]!.Count; c++)
                Assert.AreEqual(dataToWrite[r][c], valRange[r]![c]);
        }
    }
}