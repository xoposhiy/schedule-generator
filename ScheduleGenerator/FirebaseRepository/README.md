# FireBaseRepository
FireBaseRepository - репозиторий для работы с Realtime Database.

## Настройка переменных среды

Вам нужно указать только одну переменную среды:
"FirebaseSecret"

Эта переменная среды должна содержать секрет проекта firebase.

Вы должны иметь отношение к проекту и иметь доступ к информации о проекте.

Перейдите на [firebase.google.com](https://firebase.google.com/) и откройте проект, связанный с этой базой данных. База данных Firebase Realtime должна иметь название schedule-generator-5f50e.
Перейдите в Настройки -> Настройки проекта -> Сервисные аккаунты -> Секреты базы данных.



Когда вы получите секрет, создайте переменную среды с именем FirebaseSecret со значением, равным секретной строке.

Как установить переменные среды?

1. Войдите в настройки
1. Щелкните "Система".
1. Нажмите "О программе" (внизу слева).
1. Выберите Информация о системе.
1. Нажмите "Дополнительные параметры системы" слева.
1. Выберите переменные среды.
1. Нажмите «Создать» в первой половине окна.

(Настройки -> Система -> О системе -> Информация о системе -> Расширенные настройки системы -> Переменные среды -> Создать)

## Usage
```csharp

var basePath = "https://schedule-generator...";
var authSecret = Environment.GetEnvironmentVariable("FirebaseSecret");
var repo = new SessionRepository(basePath, authSecret);

var session = repo.Get(12345);

var newSession = new ScheduleSession()
{
    Id = 2
    SpreadsheetUrl = "some url34567689",
    ScheduleSheet = "forSchedule",
    InputRequirementsSheet = "inputSheet",
    RoomsSheet = "rooms",
    DialogState = DialogState.WaitSpreadsheetChangeConfirmation,
    LastModificationTime = DateTime.Now,
    LastModificationInitiator = "RTDBRepository"
};

ulong newSessionChatId = 2345625323532112212;
repo.Save(newSessionChatId, newSession);
```
