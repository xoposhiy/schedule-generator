# FireBaseRepository
FireBaseRepository is a repository for work with Realtime Database

## Setting up environment variables

You need to specify only one environment variable:
"FirebaseSecret"

This environment variable must contain the firebase project secret.

You must be related to the project and have access to the project information.

Go to [firebase.google.com](https://firebase.google.com/) and open project related to this database. Firebase Realtime Database should be schedule-generator-5f50e.
Go Settings -> Project settings -> Service accounts -> Database secrets



When you get the Secret, create an environment variable named FirebaseSecret with value equal to Secret string.

How to set environment variables?

1. Get into Settings
1. Click on System
1. Click About (on the Bottom left side)
1. Select System info
1. Click Advanced system settings on the left
1. Select Environment Variables
1. Click New in the first half of the window

(Settings -> System -> About -> System info -> Advanced system settings -> Environment Variables -> New)


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
