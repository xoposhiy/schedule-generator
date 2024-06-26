﻿using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace CommonInfrastructure.GoogleSheetsRepository
{
    public class GsRepository
    {
        public SheetsService Service { get; }
        public string[] Scopes { get; } = {SheetsService.Scope.Spreadsheets};
        public string? CurrentSheetId { get; private set; }
        public SheetInfo? CurrentSheetInfo { get; private set; }

        public GsRepository(string applicationName, string pathToCredentials, string tableUrl)
        {
            var credentials = LoadCredential(pathToCredentials);
            Service = new(new()
            {
                HttpClientInitializer = credentials,
                ApplicationName = applicationName
            });
            ChangeTable(tableUrl);
            SetUpSheetInfo();
        }

        private GoogleCredential LoadCredential(string pathToCredentials)
        {
            try
            {
                using var stream = new FileStream(pathToCredentials, FileMode.Open, FileAccess.Read);
                return GoogleCredential.FromStream(stream).CreateScoped(Scopes);
            }
            catch (FileNotFoundException)
            {
                var secret =
                    Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS", EnvironmentVariableTarget.Process);
                secret ??= Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS", EnvironmentVariableTarget.User);
                secret ??= Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS", EnvironmentVariableTarget.Machine);
                return GoogleCredential.FromJson(secret);
            }
        }

        public void ChangeTable(string url)
        {
            var urlParts = url.Split('/');
            var id = urlParts[^2];
            CurrentSheetId = id;
            SetUpSheetInfo();
        }

        public void SetUpSheetInfo()
        {
            if (CurrentSheetId is null)
                throw new("CurrentSheetId should be specified first");
            var metadata = Service.Spreadsheets
                .Get(CurrentSheetId)
                .Execute();

            CurrentSheetInfo = new(metadata);
        }

        public List<List<string?>?>? ReadCellRange(string sheetName, int top, int left, int bottom, int right)
        {
            var fullRange = GetFullRange(sheetName, top, left, bottom, right);
            return ReadCellRangeUsingStringRangeFormat(fullRange);
        }

        public List<List<string?>?>? ReadCellRangeUsingStringRangeFormat(string fullRange)
        {
            var request = Service.Spreadsheets.Values.Get(CurrentSheetId, fullRange);
            var response = request.Execute();
            var values = response.Values;
            var res = values?.Select(l => l?.Select(o => o?.ToString()).ToList()).ToList();
            return res;
        }

        public static string ConvertIndexToTableColumnFormat(int index)
        {
            var dividend = index;
            var columnName = string.Empty;

            while (dividend > 0)
            {
                var modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo) + columnName;
                dividend = (dividend - modulo) / 26;
            }

            return columnName;
        }

        public SheetModifier ModifySpreadSheet(string sheetName)
        {
            var sheetId = CurrentSheetInfo!.Sheets[sheetName];
            if (sheetId is null) throw new ArgumentException($"No sheets with name {sheetName}");
            return new(Service, CurrentSheetId!, (int) sheetId);
        }

        public void ClearCellRange(string sheetName, int top, int left, int bottom, int right)
        {
            var fullRange = GetFullRange(sheetName, top, left, bottom, right);
            var requestBody = new ClearValuesRequest();
            var deleteRequest = Service.Spreadsheets.Values.Clear(requestBody, CurrentSheetId, fullRange);
            deleteRequest.Execute();
        }

        public void CreateNewSheet(string title)
        {
            var requests = new List<Request>
            {
                new()
                {
                    AddSheet = new()
                    {
                        Properties = new()
                        {
                            Title = title,
                            TabColor = new()
                            {
                                Red = 1
                            }
                        }
                    }
                }
            };
            var requestBody = new BatchUpdateSpreadsheetRequest
            {
                Requests = requests
            };
            var request = Service.Spreadsheets.BatchUpdate(requestBody, CurrentSheetId);
            request.Execute();
        }

        public void ClearSheet(string sheetName)
        {
            ModifySpreadSheet(sheetName)
                .ClearAll()
                .UnMergeAll()
                .Execute();
        }

        private static string GetFullRange(string sheetName, int top, int left, int bottom, int right)
        {
            var leftFormatted = ConvertIndexToTableColumnFormat(left + 1);
            var rightFormatted = ConvertIndexToTableColumnFormat(right + 1);
            return $"{sheetName}!{leftFormatted}{top + 1}:{rightFormatted}{bottom + 1}";
        }
    }

    public class SheetModifier : IDisposable
    {
        private readonly List<Request> requests;
        private readonly SheetsService service;
        private readonly int sheetId;
        private readonly string spreadSheetId;

        public SheetModifier(SheetsService service, string spreadSheetId, int sheetId)
        {
            this.service = service;
            this.spreadSheetId = spreadSheetId;
            this.sheetId = sheetId;
            requests = new();
        }

        public SheetModifier WriteRange(int startRow, int startColumn, List<List<CellData>> rowsData)
        {
            var rows = rowsData.Select(row => new RowData {Values = row}).ToList();

            requests.Add(new()
            {
                UpdateCells = new()
                {
                    Start = new()
                    {
                        SheetId = sheetId,
                        RowIndex = startRow,
                        ColumnIndex = startColumn
                    },
                    Rows = rows,
                    Fields = "*"
                }
            });
            return this;
        }

        public SheetModifier MergeCell(int startRow, int startColumn, int height, int width)
        {
            requests.Add(new()
            {
                MergeCells = new()
                {
                    Range = GetRange(startRow, startColumn, height, width),
                    MergeType = "MERGE_ALL"
                }
            });

            return this;
        }

        public SheetModifier ColorizeRange(int startRow, int startColumn, int height, int width, Color color)
        {
            var commonCellData = new CellData {UserEnteredFormat = new() {BackgroundColor = color}};
            var cells = Enumerable.Repeat(commonCellData, width).ToList();
            var commonRowData = new RowData {Values = cells};
            var rows = Enumerable.Repeat(commonRowData, height).ToList();

            requests.Add(new()
            {
                UpdateCells = new()
                {
                    Range = GetRange(startRow, startColumn, height, width),
                    Rows = rows,
                    Fields = "userEnteredFormat(backgroundColor)"
                }
            });

            return this;
        }

        public SheetModifier AddNote(int startRow, int startColumn, string? note)
        {
            requests.Add(new()
            {
                UpdateCells = new()
                {
                    Range = GetRange(startRow, startColumn, 1, 1),
                    Rows = new List<RowData>
                    {
                        new()
                        {
                            Values = new List<CellData>
                            {
                                new()
                                {
                                    Note = note
                                }
                            }
                        }
                    },
                    Fields = "note"
                }
            });

            return this;
        }

        public record BordersWidths(int Top, int Bottom, int Left, int Right);

        public SheetModifier AddBorders(int startRow, int startColumn, int height = 1, int width = 1,
            BordersWidths? bordersWidths = null)
        {
            bordersWidths ??= new(1, 1, 1, 1);
            requests.Add(new()
            {
                UpdateBorders = new()
                {
                    Range = GetRange(startRow, startColumn, height, width),
                    Top = new() {Style = "SOLID", Width = bordersWidths.Top},
                    Bottom = new() {Style = "SOLID", Width = bordersWidths.Bottom},
                    Left = new() {Style = "SOLID", Width = bordersWidths.Left},
                    Right = new() {Style = "SOLID", Width = bordersWidths.Right}
                }
            });
            return this;
        }

        public SheetModifier DeleteRows(int startRow, int count)
        {
            requests.Add(new()
            {
                DeleteDimension = new()
                {
                    Range = new()
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
            requests.Add(new()
            {
                DeleteDimension = new()
                {
                    Range = new()
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
            requests.Add(new()
            {
                UnmergeCells = new()
                {
                    Range = new()
                    {
                        SheetId = sheetId
                    }
                }
            });
            return this;
        }

        public SheetModifier ClearAll()
        {
            requests.Add(new()
            {
                UpdateCells = new()
                {
                    Range = new()
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
            var requestBody = new BatchUpdateSpreadsheetRequest
            {
                Requests = requests
            };
            var request = service.Spreadsheets.BatchUpdate(requestBody, spreadSheetId);
            request.Execute();
        }

        private GridRange GetRange(int startRow, int startColumn, int height, int width)
        {
            return new()
            {
                SheetId = sheetId,
                StartRowIndex = startRow,
                StartColumnIndex = startColumn,
                EndRowIndex = startRow + height,
                EndColumnIndex = startColumn + width
            };
        }

        public void Dispose()
        {
            Execute();
        }
    }

    public class SheetInfo
    {
        public readonly string Id;
        public readonly string Url;
        public readonly Dictionary<string, int?> Sheets;
        public readonly Spreadsheet Spreadsheet;

        public SheetInfo(Spreadsheet spreadsheet)
        {
            Spreadsheet = spreadsheet;
            Id = spreadsheet.SpreadsheetId;
            Url = spreadsheet.SpreadsheetUrl;
            Sheets = spreadsheet.Sheets
                .ToDictionary(entry => entry.Properties.Title, entry => entry.Properties.SheetId);
        }
    }
}