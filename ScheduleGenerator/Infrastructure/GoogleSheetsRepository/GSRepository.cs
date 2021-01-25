using System;
using System.Collections.Generic;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System.IO;
using System.Linq;

namespace Infrastructure.GoogleSheetsRepository
{
    public class GSRepository
    {
        public GoogleCredential Credentials { get; private set; }
        public SheetsService Service { get; private set; }
        public string[] Scopes { get; private set; }
        public string ApplicationName { get; private set; }
        public string CurrentSheetId { get; private set; }
        public SheetInfo CurrentSheetInfo { get; private set; }
        public GSRepository(string applicationName, string pathToCredentials, string tableURL)
        {
            Scopes = new string[] { SheetsService.Scope.Spreadsheets };
            ApplicationName = applicationName;
            SetUpCredential(pathToCredentials);
            SetUpDefaultService();
            CurrentSheetId = null;
            CurrentSheetInfo = null;
            ChangeTable(tableURL);
        }

        private void SetUpCredential(string pathToCredentials)
        {
            using (var stream = new FileStream(pathToCredentials, FileMode.Open, FileAccess.Read))
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

        public void ChangeTable(string url)
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

        public string ReadCell(string sheetName, ValueTuple<int, int> cellCoords)
        {
            return ReadOneCellAsObject(sheetName, cellCoords)?.ToString();
        }

        public List<List<string>> ReadCellRange(string sheetName, ValueTuple<int, int> rangeStart, ValueTuple<int, int> rangeEnd)
        {
            var (top, leftIndex) = rangeStart;
            var (bottom, rightIndex) = rangeEnd;
            leftIndex++;
            top++;
            rightIndex++;
            bottom++;
            var left = ConvertIndexToTableColumnFormat(leftIndex);
            var right = ConvertIndexToTableColumnFormat(rightIndex);
            var range = $"{left}{top}:{right}{bottom}";
            var values = ReadCellRangeUsingStringRangeFormat(sheetName, range);
            return values;
        }

        private object ReadOneCellAsObject(string sheetName, ValueTuple<int, int> rangeStart)
        {
            var (top, leftIndex) = rangeStart;
            leftIndex++;
            top++;
            var left = ConvertIndexToTableColumnFormat(leftIndex);
            var range = $"{left}{top}";
            var values = ReadCellRangeUsingStringRangeFormat(sheetName, range);
            var value = values?.First()?.First();
            return value;
        }

        public List<List<string>> ReadCellRangeUsingStringRangeFormat(string sheetName, string range)
        {
            var fullRange = string.Format("{0}!{1}", sheetName, range);
            var request = Service.Spreadsheets.Values.Get(CurrentSheetId, fullRange);
            var response = request.Execute();
            var values = response.Values;
            var res = values?.Select(l => l?.Select(o => o?.ToString()).ToList()).ToList();
            return res;
        }

        public static string ConvertIndexToTableColumnFormat(int index)
        {
            int dividend = index;
            var columnName = string.Empty;
            int modulo;

            while (dividend > 0)
            {
                modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo).ToString() + columnName;
                dividend = (int)((dividend - modulo) / 26);
            }

            return columnName;
        }

        public SheetModifier ModifySpreadSheet(string sheetName)
        {
            var sheetId = CurrentSheetInfo.Sheets[sheetName];
            if (sheetId is null)
            {
                throw new ArgumentException($"No sheets with name {sheetName}");
            }
            return new SheetModifier(Service, CurrentSheetId, (int)sheetId);
        }

        public void ClearCellRange(string sheetName, ValueTuple<int, int> rangeStart, ValueTuple<int, int> rangeEnd)
        {
            var (top, leftIndex) = rangeStart;
            var (bottom, rightIndex) = rangeEnd;
            leftIndex++;
            top++;
            rightIndex++;
            bottom++;
            var left = ConvertIndexToTableColumnFormat(leftIndex);
            var right = ConvertIndexToTableColumnFormat(rightIndex);
            var range = $"{left}{top}:{right}{bottom}";
            String fullRange = String.Format("{0}!{1}", sheetName, range);
            var requestBody = new ClearValuesRequest();
            var deleteRequest = Service.Spreadsheets.Values.Clear(requestBody, CurrentSheetId, fullRange);
            var deleteResponse = deleteRequest.Execute();
        }

        public void CreateNewSheet(string title)
        {
            var requests = new List<Request>();
            requests.Add(new Request()
            {
                AddSheet = new AddSheetRequest
                {
                    Properties = new SheetProperties
                    {
                        Title = title,
                        TabColor = new Color()
                        {
                            Red = 1
                        }
                    }
                }
            });
            var requestBody = new BatchUpdateSpreadsheetRequest();
            requestBody.Requests = requests;
            var request = Service.Spreadsheets.BatchUpdate(requestBody, CurrentSheetId);
            var response = request.Execute();
        }
    }

    public class SheetModifier
    {

        private List<Request> requests;
        private SheetsService service;
        private int sheetId;
        private string spreadSheetId;
        public SheetModifier(SheetsService service, string spreadSheetId, int sheetId)
        {

            this.service = service;
            this.spreadSheetId = spreadSheetId;
            this.sheetId = sheetId;
            requests = new List<Request>();
        }

        public SheetModifier WriteRange(ValueTuple<int, int> leftTop, List<List<string>> payload)
        {
            var (topIndex, leftIndex) = leftTop;
            var rows = new List<RowData>();
            foreach (var row in payload)
            {
                var cells = new List<CellData>();
                foreach (var value in row)
                {
                    cells.Add(new CellData
                    {
                        UserEnteredValue = new ExtendedValue
                        {
                            StringValue = value
                        }
                    });
                }
                rows.Add(
                    new RowData
                    {
                        Values = cells
                    }
                );
            }
            requests.Add(new Request()
            {
                UpdateCells = new UpdateCellsRequest
                {
                    Start = new GridCoordinate
                    {
                        SheetId = sheetId,
                        RowIndex = topIndex,
                        ColumnIndex = leftIndex
                    },
                    Rows = rows,
                    Fields = "*"
                }
            });
            return this;
        }

        public SheetModifier MergeCell(ValueTuple<int, int> rangeStart, ValueTuple<int, int> rangeEnd)
        {
            var (top, leftIndex) = rangeStart;
            var (bottom, rightIndex) = rangeEnd;
            requests.Add(new Request()
            {
                MergeCells = new MergeCellsRequest
                {
                    Range = new GridRange
                    {
                        SheetId = sheetId,
                        StartRowIndex = top,
                        StartColumnIndex = leftIndex,
                        EndRowIndex = bottom + 1,
                        EndColumnIndex = rightIndex + 1
                    },
                    MergeType = "MERGE_ALL"
                }
            });

            return this;
        }

        public SheetModifier IncertRows(int startRow, int count)
        {
            requests.Add(new Request()
            {
                InsertDimension = new InsertDimensionRequest
                {
                    Range = new DimensionRange
                    {
                        SheetId = sheetId,
                        StartIndex = startRow,
                        EndIndex = startRow + count,
                        Dimension = "ROWS"
                    }
                }
            });

            return this;
        }

        public SheetModifier IncertColumns(int startColumn, int count)
        {
            requests.Add(new Request()
            {
                InsertDimension = new InsertDimensionRequest
                {
                    Range = new DimensionRange
                    {
                        SheetId = sheetId,
                        StartIndex = startColumn,
                        EndIndex = startColumn + count,
                        Dimension = "COLUMNS"
                    }
                }
            });

            return this;
        }

        public SheetModifier ColorizeRange(ValueTuple<int, int> rangeStart, ValueTuple<int, int> rangeEnd, Color color)
        {
            var (top, left) = rangeStart;
            var (bottom, right) = rangeEnd;
            // new
            bottom++;
            right++;
            var rows = new List<RowData>();
            for (int r = top; r < bottom; r++)
            {
                var cells = new List<CellData>();
                for (int c = left; c < right; c++)
                {
                    cells.Add(new CellData
                    {
                        UserEnteredFormat = new CellFormat
                        {
                            BackgroundColor = color
                        }

                    });
                }
                rows.Add(new RowData
                {
                    Values = cells
                });
            }
            requests.Add(new Request()
            {
                UpdateCells = new UpdateCellsRequest
                {
                    Range = new GridRange
                    {
                        SheetId = sheetId,
                        StartRowIndex = top,
                        StartColumnIndex = left,
                        EndRowIndex = bottom,
                        EndColumnIndex = right
                    },
                    Rows = rows,
                    Fields = "userEnteredFormat(backgroundColor)"
                }
            });

            return this;
        }

        public SheetModifier AddComment(ValueTuple<int, int> rangeStart, string comment)
        {
            var (top, leftIndex) = rangeStart;
            requests.Add(new Request()
            {
                UpdateCells = new UpdateCellsRequest
                {
                    Range = new GridRange
                    {
                        SheetId = sheetId,
                        StartRowIndex = top,
                        StartColumnIndex = leftIndex,
                        EndRowIndex = top + 1,
                        EndColumnIndex = leftIndex + 1
                    },
                    Rows = new List<RowData>() {
                        new RowData {
                            Values = new List<CellData>{
                                new CellData{
                                    Note = comment
                                }
                            }
                        }
                    },
                    Fields = "note"
                }
            });

            return this;
        }


        public SheetModifier AddBorders(ValueTuple<int, int> rangeStart, ValueTuple<int, int> rangeEnd, Color color)
        {
            var (top, leftIndex) = rangeStart;
            var (bottom, rightIndex) = rangeEnd;
            requests.Add(new Request()
            {
                UpdateBorders = new UpdateBordersRequest
                {
                    Range = new GridRange
                    {
                        SheetId = sheetId,
                        StartRowIndex = top,
                        StartColumnIndex = leftIndex,
                        EndRowIndex = bottom + 1,
                        EndColumnIndex = rightIndex + 1
                    },
                    Top = new Border
                    {
                        Color = color,
                        Style = "SOLID"
                    },
                    Bottom = new Border
                    {
                        Color = color,
                        Style = "SOLID"
                    },
                    Left = new Border
                    {
                        Color = color,
                        Style = "SOLID"
                    },
                    Right = new Border
                    {
                        Color = color,
                        Style = "SOLID"
                    }
                }
            });
            return this;
        }

        public SheetModifier DeleteRows(int startRow, int count)
        {
            requests.Add(new Request()
            {
                DeleteDimension = new DeleteDimensionRequest()
                {
                    Range = new DimensionRange
                    {
                        SheetId = sheetId,
                        Dimension = "ROWS",
                        StartIndex = startRow,
                        EndIndex = startRow + count
                    }
                }
            });
            return this;
        }

        public SheetModifier DeleteColumns(int startRow, int count)
        {
            requests.Add(new Request()
            {
                DeleteDimension = new DeleteDimensionRequest()
                {
                    Range = new DimensionRange
                    {
                        SheetId = sheetId,
                        Dimension = "COLUMNS",
                        StartIndex = startRow,
                        EndIndex = startRow + count
                    }
                }
            });
            return this;
        }

        public SheetModifier UnMergeAll()
        {
            requests.Add(new Request()
            {
                UnmergeCells = new UnmergeCellsRequest()
                {
                    Range = new GridRange()
                    {
                        SheetId = sheetId
                    }
                }
            });
            return this;
        }

        public SheetModifier ClearAll()
        {
            requests.Add(new Request()
            {
                UpdateCells = new UpdateCellsRequest
                {
                    Range = new GridRange
                    {
                        SheetId = sheetId
                    },
                    Fields = "*"
                }
            });
            return this;
        }


        public void Execute()
        {
            var requestBody = new BatchUpdateSpreadsheetRequest();
            requestBody.Requests = requests;
            var request = service.Spreadsheets.BatchUpdate(requestBody, spreadSheetId);
            var response = request.Execute();
        }
    }

    public class SheetInfo
    {
        public readonly string Id;
        public readonly string Url;
        public readonly Dictionary<string, int?> Sheets;
        public readonly Spreadsheet spreadsheet;
        public SheetInfo(Spreadsheet spreadsheet)
        {
            this.spreadsheet = spreadsheet;
            Id = spreadsheet.SpreadsheetId;
            Url = spreadsheet.SpreadsheetUrl;
            Sheets = spreadsheet.Sheets
                .ToDictionary(entry => entry.Properties.Title, entry => entry.Properties.SheetId);
        }
    }
}
