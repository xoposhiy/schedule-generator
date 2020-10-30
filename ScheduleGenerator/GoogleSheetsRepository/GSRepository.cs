using System;
using System.Collections.Generic;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System.IO;
using System.Linq;

namespace GoogleSheetsRepository
{
    class GSRepository
    {
        private string CredentialsEnvVar = "GoogleApiCredentials";
        public GoogleCredential Credentials { get; private set; }
        public SheetsService Service { get; private set; }
        public string[] Scopes { get; private set; }
        public string ApplicationName { get; private set; }
        public string CurrentSheetId { get; private set; }
        public SheetInfo CurrentSheetInfo { get; private set; }
        public bool ZeroIndexing;
        public GSRepository(string[] scopes, string applicationName)
        {
            Scopes = scopes;
            ApplicationName = applicationName;
            SetUpDefaultCredential();
            SetUpDefaultService();
            CurrentSheetId = null;
            CurrentSheetInfo = null;
            ZeroIndexing = false;
        }

        public GSRepository(string[] scopes, string applicationName, bool zeroIndexing) : this(scopes, applicationName)
        {
            ZeroIndexing = zeroIndexing;
        }

        private void SetUpDefaultCredential()
        {
            var credentialDirPath = Environment.GetEnvironmentVariable(CredentialsEnvVar);
            if (credentialDirPath is null)
                throw new Exception($"Credential path not found (check env var {CredentialsEnvVar})");
            var credentialPath = credentialDirPath + "\\client_secrets.json"; // Поменял переменную, теперь она без файла

            using (var stream = new FileStream(credentialPath, FileMode.Open, FileAccess.Read))
            {
                Credentials = GoogleCredential.FromStream(stream)
                    .CreateScoped(Scopes);
            }
        }

        private void SetUpDefaultService()
        {
            Service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = Credentials,
                ApplicationName = ApplicationName
            });
        }

        public void Use(string url)
        {
            var urlParts = url.Split('/');
            var id = urlParts[urlParts.Length - 2];
            CurrentSheetId = id;
            SetUpSheetInfo();
        }

        public void SetUpSheetInfo()
        {
            if (CurrentSheetId is null)
                throw new Exception("CurrentSheetId should be specified first");
            var metadata = Service.Spreadsheets
                .Get(CurrentSheetId)
                .Execute();

            CurrentSheetInfo = new SheetInfo(metadata);
        }

        public List<string> GetSheetNames(string id)
        {
            var metadata = Service.Spreadsheets.Get(id).Execute();
            var sheets = metadata.Sheets;
            var sheetTitles = sheets.Select(x => x.Properties.Title);
            return sheetTitles.ToList();
        }

        public string[,] ReadRange(string sheetName, ValueTuple<int, int> rangeStart, ValueTuple<int, int> rangeEnd)
        {
            var rowValues = ReadRow(sheetName, rangeStart, rangeEnd);
            var resultValues = new string[rowValues.Count, rowValues.First().Count];
            for (int r = 0; r < rowValues.Count; r++)
            {
                var row = rowValues[r];
                for (int c = 0; c < row.Count; c++)
                {
                    resultValues[r, c] = row[c].ToString();
                }
            }

            return resultValues;
        }

        public IList<IList<Object>> ReadRow(string sheetName, ValueTuple<int, int> rangeStart, ValueTuple<int, int> rangeEnd)
        {
            var (leftIndex, top) = rangeStart;
            var (rightIndex, bottom) = rangeEnd;
            if (ZeroIndexing)
            {
                leftIndex++;
                top++;
                rightIndex++;
                bottom++;
            }
            var left = ConvertToTableIndex(leftIndex);
            var right = ConvertToTableIndex(rightIndex);
            var range = $"{left}{top}:{right}{bottom}";
            var values = ReadRowStringRange(sheetName, range);
            return values;
        }

        public Object ReadRow(string sheetName, ValueTuple<int, int> rangeStart)
        {
            var (leftIndex, top) = rangeStart;
            if (ZeroIndexing)
            {
                leftIndex++;
                top++;
            }
            var left = ConvertToTableIndex(leftIndex);
            var range = $"{left}{top}";
            var values = ReadRowStringRange(sheetName, range);
            var value = values.First().First();
            return value;
        }

        public IList<IList<Object>> ReadRowStringRange(string sheetName, string range)
        {
            String fullRange = String.Format("{0}!{1}", sheetName, range);
            var request = Service.Spreadsheets.Values.Get(CurrentSheetId, fullRange);
            var response = request.Execute();
            var values = response.Values;
            return values;
        }

        public static string ConvertToTableIndex(int index)
        {
            int dividend = index;
            string columnName = String.Empty;
            int modulo;

            while (dividend > 0)
            {
                modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo).ToString() + columnName;
                dividend = (int)((dividend - modulo) / 26);
            }

            return columnName;
        }

        public void WriteRange(string pageName, ValueTuple<int, int> leftTop, List<List<string>> payload)
        {
            var (leftIndex, topIndex) = leftTop;
            if (!ZeroIndexing)
            {
                leftIndex--;
                topIndex--;
            }
            var rowDatas = new List<RowData>();
            foreach (var row in payload)
            {
                var cellDatas = new List<CellData>();
                foreach (var value in row)
                {
                    cellDatas.Add(new CellData
                    {
                        UserEnteredValue = new ExtendedValue
                        {
                            StringValue = value
                        }
                    });
                }
                rowDatas.Add(
                    new RowData
                    {
                        Values = cellDatas
                    }
                );
            }
            var requests = new List<Request>();
            requests.Add(new Request()
            {
                UpdateCells = new UpdateCellsRequest
                {
                    Start = new GridCoordinate
                    {
                        SheetId = CurrentSheetInfo.Sheets[pageName],
                        RowIndex = topIndex,
                        ColumnIndex = leftIndex
                    },
                    Rows = rowDatas,
                    Fields = "*"
                }
            });

            var requestBody = new BatchUpdateSpreadsheetRequest();
            requestBody.Requests = requests;

            var request = Service.Spreadsheets.BatchUpdate(requestBody, CurrentSheetId);
            var response = request.Execute();
        }
    }

    public class SheetInfo
    {
        public readonly string Id;
        public readonly string Url;
        public readonly Dictionary<string, int?> Sheets;
        public SheetInfo(Spreadsheet spreadsheet)
        {
            Id = spreadsheet.SpreadsheetId;
            Url = spreadsheet.SpreadsheetUrl;
            Sheets = spreadsheet.Sheets
                .ToDictionary(entry => entry.Properties.Title, entry => entry.Properties.SheetId);
        }
    }
}
