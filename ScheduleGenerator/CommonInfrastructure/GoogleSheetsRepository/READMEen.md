# GoogleSheetsRepository

GoogleSheetsRepository is a repository for work with Google Sheets

## Setting up environment variables

You need to specify only one environment variable:
"GoogleApiCredentials"

This environment variable must contain the path to the folder where the credentials are located.

Credentials is a file called "client_secrets.json"

You can create Credentials yourself or request them from the repository owner if you are related with the project.

When you get the client_secrets.json file, put it in a folder of your choice and create an environment variable with the
path to that folder.

How to set environment variables?

1. Get into Settings
1. Click on System
1. Click About (on the Bottom left side)
1. Select System info
1. Click Advanced system settings on the left
1. Select Environment Variables
1. Click New in the first half of the window

(Settings -> System -> About -> System info -> Advanced system settings -> Environment Variables -> New)

Create a variable named GoogleApiCredentials with value equal to path to the folder with Credentials.

## Usage

```csharp

var ApplicationName = "MyApp";
var credentialDirPath = Environment.GetEnvironmentVariable("GoogleApiCredentials");
var credentialPath = credentialDirPath + "\\client_secrets.json";
var url = "https://docs.google.com/spreadsheets/...";
var repo = new GSRepository(ApplicationName, credentialPath, url);
IList<IList<string>> data = repo.ReadRow(repo.CurrentSheetInfo.Sheets.Keys.First(), (1, 1));


var dataToWrite = new List<List<string>>()
{
    new List<string>() { "11", "12" },
    new List<string>() { "21", "22"},
    new List<string>() { "31", "32"},
};

repo.ModifySpreadSheet(repo.CurrentSheetInfo.Sheets.Keys.First())
                .WriteRange((1, 2), dataToWrite)
                .Execute();
```
