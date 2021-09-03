using System;
using Domain.Conversions;
using Infrastructure.GoogleSheetsRepository;
using NUnit.Framework;

namespace Testing.ConversionsTests
{
    class SheetToRequisitionConversionTest
    {
        private static string CredentialsEnvVar = "GoogleApiCredentials";
        private static readonly string ApplicationName = "MyApp";

        [Test]
        public void Test1() {
            var credentialDirPath = Environment.GetEnvironmentVariable(CredentialsEnvVar);
            var credentialPath = credentialDirPath + "\\client_secrets.json";
            var url = "https://docs.google.com/spreadsheets/d/1-CFNA5rFSKmrs5jdJm5xg2b1cQDbCJ8LA1FRrdBBRyg/edit#gid=1607674973";
            var repo = new GSRepository(ApplicationName, credentialPath, url);

            var requisitions = SheetToRequisitionConverter.ConvertToRequisitions(
                repo, 
                "Requisition", 
                "Plan", 
                "");
            
            //TODO update expected counts
            Assert.AreEqual(10, requisitions.Item1.Count);
            Assert.AreEqual(10, requisitions.Item2.Items.Length);
            Assert.AreEqual(10, requisitions.Item3.Count);
        }
    }
}
