# GoogleSheetsRepository
GoogleSheetsRepository - репозиторий для работы с Google Sheets

## Настройка переменных среды

Вам нужно указать только одну переменную среды:
"GoogleApiCredentials"

Эта переменная среды должна содержать путь к папке, в которой находятся учетные данные.

Учетные данные - это файл с именем client_secrets.json.

Вы можете создать учетные данные самостоятельно или запросить их у владельца репозитория, если вы связаны с проектом.

Когда вы получите файл client_secrets.json, поместите его в папку по вашему выбору и создайте переменную среды с путем к этой папке.

Как установить переменные среды?

1. Войдите в настройки
1. Щелкните "Система".
1. Нажмите "О системе" (внизу слева).
1. Выберите Информация о системе.
1. Нажмите "Дополнительные параметры системы" слева.
1. Выберите переменные среды.
1. Нажмите «Создать» в первой половине окна.

(Настройки -> Система -> О системе -> Информация о системе -> Расширенные настройки системы -> Переменные среды -> Создать)

Создайте переменную с именем GoogleApiCredentials со значением, равным пути к папке с учетными данными.


Как создать учетные данные?
1. Перейдите по ссылке [console.developers.google.com](https://console.developers.google.com/).
1. Если у вас нет подходящего проекта создайте его нажав "CREATE A PROJECT". Придумайте имя проекта и нажмите "CREATE"
1. Нажмите "ENABLE APIS AND SERVICES"
1. Найдите и нажмите Google Sheets API
1. Нажмите "ENABLE"
1. Нажмите "CREATE CREDENTIALS"
1. Выберите тип нажав на выпадающий список "Choose" и выберите "Google Sheets API" (в Which API are you using?)
1. Выберите Web server (в Where will you be calling the API from?)
1. Выберите "Application data" в пункте выбора типа данных (в What data will you be accessing?)
1. Выберите "No, I'm not using them" (в Are you planning to use this API with App Engine or Compute Engine?)
1. Нажмите на "Which credentials do I need?" 
1. Напишите имя аккаунта сервиса
1. Выберите роль (Role) Project -> Owner
1. Выберите тип ключа JSON
1. Нажмите "Continue"
1. Учетные данные должны начать загружаться на ваш компьютер


## Usage
```csharp

var Scopes = { SheetsService.Scope.Spreadsheets };
var ApplicationName = "MyApp";
var repo = new GSRepository(Scopes, ApplicationName);
repo.Use("https://docs.google.com/spreadsheets/...");
IList<IList<string>> data = repo.ReadRow(repo.CurrentSheetInfo.Sheets.Keys.First(), (1, 1));


var dataToWrite = new List<List<string>>()
{
    new List<string>() { "11", "12" },
    new List<string>() { "21", "22"},
    new List<string>() { "31", "32"},
};

repo.WriteRange(repo.CurrentSheetInfo.Sheets.Keys.First(), (1, 2), dataToWrite);
```
