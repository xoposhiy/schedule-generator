using Domain.Conversions;
using Infrastructure.GoogleSheetsRepository;
using NUnit.Framework;

namespace Testing.ConversionsTests
{
    [TestFixture]
    internal class SheetToRequisitionConversionTest
    {
        private const string Url =
            "https://docs.google.com/spreadsheets/d/1Q9imoj8xLFgp887NsYeW8ngJ53E5GHvKblrnfatEBHk/edit#gid=";

        [Test]
        public void RequisitionsReadTest()
        {
            const string credentialPath = "..\\..\\..\\..\\Credentials\\client_secrets.json";
            var repo = new GsRepository("test", credentialPath, Url);

            var requisitions = SheetToRequisitionConverter.ConvertToRequisitions(
                repo,
                "Входные требования",
                "Учебный план",
                "Аудитории");

            Assert.AreEqual(63, requisitions.Item1.Count);
            Assert.AreEqual(24, requisitions.Item2.Items.Length);
            Assert.AreEqual(24, requisitions.Item3.Count);
        }
    }
}