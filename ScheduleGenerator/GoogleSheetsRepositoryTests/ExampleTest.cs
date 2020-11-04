using NUnit.Framework;
using GoogleSheetsRepository;
using Google.Apis.Sheets.v4;

using System;
using System.Linq;
using System.Collections.Generic;

namespace GoogleSheetsRepositoryTests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var ApplicationName = "MyApp";
            var credentialDirPath = Environment.GetEnvironmentVariable("GoogleApiCredentials");
            var credentialPath = credentialDirPath + "\\client_secrets.json";
            var url = "https://docs.google.com/spreadsheets/d/1ncJ77JJJapPJpTeDpvWIsmuavStz5aXwQLs7fc89Rgo/edit#gid=0";
            var repo = new GSRepository(ApplicationName, credentialPath, url);

            var dataToWrite = new List<List<string>>()
            {
                new List<string>() { "11", "12" },
                new List<string>() { "21", "22"},
                new List<string>() { "31", "32"},
            };
            repo.WriteRange(repo.CurrentSheetInfo.Sheets.Keys.First(), (1, 2), dataToWrite);

            var valRange = repo.ReadRange(repo.CurrentSheetInfo.Sheets.Keys.First(), (1, 2), (3, 5));
            for (int r = 0; r < valRange.GetLength(0); r++)
            {
                for (int c = 0; c < valRange.GetLength(1); c++)
                {
                    Assert.AreEqual(dataToWrite[r][c], valRange[r, c]);
                }
            }
        }
    }
}