using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using Newtonsoft.Json;

using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Domain.GoogleSheetsRepository;
using Domain.FirebaseRepository;

namespace Bot
{
    public class TBot
    {
        private TelegramBotClient client;
        private string repoSecret;
        private Regex LinkRegex = new Regex("https://docs.google.com/spreadsheets/d/([a-zA-Z0-9-_]+)");
        private GSRepository repo;
        private SessionRepository sessionRepository;
        private string credentialAddressToShare;
        private Dictionary<long, ScheduleSession> sessionDict;
        private Dictionary<long, AdditionalSessionState> additionalStateDict;
        public TBot(string token, string repoSecret, string firebaseSecret, string dbBasePath)
        {
            client = new TelegramBotClient(token);
            sessionRepository = new SessionRepository(dbBasePath, firebaseSecret);
            sessionDict = new Dictionary<long, ScheduleSession>();
            additionalStateDict = new Dictionary<long, AdditionalSessionState>();
            this.repoSecret = repoSecret;
            client.OnMessage += BotOnMessageReceived;
            client.OnMessageEdited += BotOnMessageReceived;

            var readedString = System.IO.File.ReadAllText(repoSecret);
            var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(readedString);
            credentialAddressToShare = values["client_email"];
        }

        public void Start()
        {
            client.StartReceiving();
        }

        public void Stop()
        {
            client.StopReceiving();
            // Session serialization
            foreach (var chatIdSession in sessionDict)
            {
                sessionRepository.Save(chatIdSession.Key, chatIdSession.Value);
            }
        }

        private async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;
            var chatID = message.Chat.Id;
            var isNew = false;
            if (!sessionDict.ContainsKey(chatID))
            {
                isNew = GetScheduleSessionByChatId(chatID, out var scheduleSession);
                sessionDict[chatID] = scheduleSession;
            }

            var currentSession = sessionDict[chatID];


            if (!additionalStateDict.ContainsKey(chatID))
            {
                isNew = GetAdditionalSessionStateByChatId(chatID, currentSession, out var additionalSessionState);
                additionalStateDict[chatID] = additionalSessionState;
            }
            var currentAdditionalState = additionalStateDict[chatID];


            if (message?.Type == MessageType.Text)
            {
                if (message.Text == "/help" || message.Text == "Помощь")
                {
                    ShowHelp(chatID);
                }
                else if (message.Text == "/restart" || message.Text == "Заново")
                {
                    RestartSessionFor(chatID);
                }
                else if (string.IsNullOrEmpty(currentSession.SpreadsheetUrl))
                {
                    HandleSheetUrlAnswerAndAskForAccessIfSuccess(chatID, message.Text, currentSession, isNew);
                }
                else if (!currentAdditionalState.AccessRecieved)
                {
                    CheckAccessAndAskForRequesitionSheetIfSuccess(chatID, message.Text, currentSession, currentAdditionalState);
                }
                else if (string.IsNullOrEmpty(currentSession.InputRequirementsSheet))
                {
                    HandleRequesitionSheetAndAskForLearningPlanIfSuccess(chatID, message.Text, currentSession);
                }
                else if (string.IsNullOrEmpty(currentSession.RoomsSheet))
                {
                    HandleLearningPlanSheetAndAskForRoomSheetIfSuccess(chatID, message.Text, currentSession);
                }
                else if (!currentAdditionalState.DataIsValid)
                {
                    HandleDataValidationAndAskForOutputSheetIfSuccess(chatID, message.Text, currentAdditionalState);
                }
                else if (string.IsNullOrEmpty(currentSession.ScheduleSheet))
                {
                    HandleScheduleSheetAndCreateSchedule(chatID, message.Text, currentSession, currentAdditionalState);
                }
                else
                {
                    if (currentAdditionalState.CreatingSchedule)
                    {
                        var answer = "Составляю расписание. Ожидайте.";
                        await client.SendTextMessageAsync(chatID, answer);
                    }

                    // Save current session
                    sessionRepository.Save(chatID, currentSession);
                    await client.SendTextMessageAsync(chatID, "Кажется, что предыдущая сессия уже заверщиласть составлением расписания.\n" +
                        "Напишите \"Заново\" или /restart, чтобы начать сначала.");
                }
            }
        }

        private bool GetScheduleSessionByChatId(long chatID, out ScheduleSession scheduleSession)
        {
            var justCreated = false;
            // Ask Firebase
            //   if found check access to spreadsheet and set what access already recieved
            scheduleSession = sessionRepository.Get(chatID);
            if (scheduleSession == null)
            {
                // If not found in db
                scheduleSession = new ScheduleSession();
                scheduleSession.Id = chatID;
                scheduleSession.LastModificationInitiator = "TelegramBot";
                scheduleSession.LastModificationTime = DateTime.Now;
                scheduleSession.DialogState = DialogState.Initial;
                justCreated = true;
            }

            return justCreated;
        }

        private bool GetAdditionalSessionStateByChatId(long chatID, ScheduleSession currentScheduleSession,
                out AdditionalSessionState additionalSessionState)
        {
            var isFirstTime = true;
            additionalSessionState = new AdditionalSessionState(chatID);
            if (!string.IsNullOrEmpty(currentScheduleSession.InputRequirementsSheet))
            {
                try
                {
                    repo = new GSRepository("PsA32710i", repoSecret, currentScheduleSession.SpreadsheetUrl);
                    additionalSessionState.AccessRecieved = true;
                    isFirstTime = false;
                }
                catch
                {
                    //await client.SendTextMessageAsync(chatID, "Доступ не выдан. Попробуйте снова");
                    additionalSessionState.AccessRecieved = false;
                }
                if (!string.IsNullOrEmpty(currentScheduleSession.ScheduleSheet))
                {
                    additionalSessionState.DataIsValid = true;
                }
            }

            return isFirstTime;
        }



        private async void ShowHelp(long chatID)
        {
            var answer = "Я Бот для составления расписания.\n Чтобы начать сначала введите \"Заново\" или /restart";
            await client.SendTextMessageAsync(chatID, answer);
        }

        private async void RestartSessionFor(long chatID)
        {
            sessionDict.Remove(chatID);
            additionalStateDict.Remove(chatID);
            sessionRepository.Delete(chatID);
            var answer = "Начинаем все сначала.";
            await client.SendTextMessageAsync(chatID, answer);
        }

        private async void HandleSheetUrlAnswerAndAskForAccessIfSuccess(long chatID, string message,
                ScheduleSession scheduleSession, bool isChatFresh)
        {
            if (LinkRegex.IsMatch(message))
            {
                scheduleSession.SpreadsheetUrl = message;
                scheduleSession.LastModificationTime = DateTime.Now;
                var answer = "URL получен. Дайте мне доступ на редактирование. Добавьте мой адрес в редакторы вашей таблицы.\n";
                answer += $"Нажмите на кнопку \"Готово\" когда сделаете.\n";
                var keyboard = CreateKeyboard(new List<string> { "Готово" }, 1);

                answer += $"Вот мой адрес: {credentialAddressToShare}";
                await client.SendTextMessageAsync(chatID, answer, ParseMode.Default, false, false, 0, keyboard);
            }
            else
            {
                if (isChatFresh)
                {
                    var answer = "Привет. Я бот для создания расписаний. Чтобы начать, отправьте сюда " +
                        "ссылку на Spreadsheet (url для таблицы в Google Sheets)";
                    await client.SendTextMessageAsync(chatID, answer);
                }
                else
                {
                    var answer = "Не понимаю. Сначала скиньте ссылку на Spreadsheet (url для таблицы" +
                        " в Google Sheets). Просто отправьте суда текстом.";
                    await client.SendTextMessageAsync(chatID, answer);
                }
            }
        }

        private async void CheckAccessAndAskForRequesitionSheetIfSuccess(long chatID, string message,
                ScheduleSession scheduleSession, AdditionalSessionState additionalSessionState)
        {
            if (message == "Готово")
            {
                // Access check
                try
                {
                    repo = new GSRepository("PsA32710i", repoSecret, scheduleSession.SpreadsheetUrl);
                }
                catch
                {
                    await client.SendTextMessageAsync(chatID, "Доступ не выдан. Попробуйте снова.");
                    return;
                }
                // If access accuired
                var answer = "Доступ получен. " +
                    "На каком листе таблицы будут пожелания по расписанию от преподавателей?";
                additionalSessionState.AccessRecieved = true;
                // Buttons apiare

                repo.SetUpSheetInfo();
                var sheetNames = repo.CurrentSheetInfo.Sheets.Keys.ToList();
                sheetNames.Add("Создать");
                var keyboard = CreateKeyboard(sheetNames, 6);

                await client.SendTextMessageAsync(chatID, answer, ParseMode.Default, false, false, 0, keyboard);
            }
            else
            {
                var answer = "Дайте мне доступ на редактирование. Добавьте мой адрес в редакторы вашей таблицы.\n" +
                    $"Вот мой адрес: {credentialAddressToShare}";
                await client.SendTextMessageAsync(chatID, answer);
            }
        }

        public async void HandleRequesitionSheetAndAskForLearningPlanIfSuccess(long chatID, string message,
                ScheduleSession scheduleSession)
        {
            var exists = false;
            // If specified "Create", create 
            if (message == "Создать")
            {
                repo.SetUpSheetInfo();
                var takenNames = repo.CurrentSheetInfo.Sheets.Keys.ToList();
                var newSheetName = FindUniqueName(takenNames, "Requestion");
                repo.CreateNewSheet(newSheetName);
                scheduleSession.InputRequirementsSheet = newSheetName;
                scheduleSession.LastModificationTime = DateTime.Now;
                exists = true;
            }
            // Check what this sheet really exists
            else
            {
                repo.SetUpSheetInfo();
                var takenNames = repo.CurrentSheetInfo.Sheets.Keys.ToList();
                if (takenNames.Contains(message))
                {
                    scheduleSession.InputRequirementsSheet = message;
                    scheduleSession.LastModificationTime = DateTime.Now;
                    exists = true;
                }
            }

            if (exists)
            {

                var answer = $"Хорошо, таблица найдена (или создана) \"{scheduleSession.InputRequirementsSheet}\"." +
                    $" Теперь укажите имя таблицы с учебным планом";
                // keyboard update
                repo.SetUpSheetInfo();
                var sheetNames = repo.CurrentSheetInfo.Sheets.Keys.ToList();
                sheetNames.Add("Создать");
                var keyboard = CreateKeyboard(sheetNames, 6);

                await client.SendTextMessageAsync(chatID, answer, ParseMode.Default, false, false, 0, keyboard);
            }
            else
            {
                var answer = "Не нашел такой таблицы. Попробуйте снова.";
                await client.SendTextMessageAsync(chatID, answer);
            }
        }

        public async void HandleLearningPlanSheetAndAskForRoomSheetIfSuccess(long chatID, string message,
                ScheduleSession scheduleSession)
        {
            var exists = false;
            // If specified "Create", create 
            if (message == "Создать")
            {
                repo.SetUpSheetInfo();
                var takenNames = repo.CurrentSheetInfo.Sheets.Keys.ToList();
                var newSheetName = FindUniqueName(takenNames, "LearningPlan");
                repo.CreateNewSheet(newSheetName);
                scheduleSession.RoomsSheet = newSheetName;
                scheduleSession.LastModificationTime = DateTime.Now;
                exists = true;
            }
            // Check what this sheet really exists
            else
            {
                repo.SetUpSheetInfo();
                var takenNames = repo.CurrentSheetInfo.Sheets.Keys.ToList();
                if (takenNames.Contains(message))
                {
                    scheduleSession.RoomsSheet = message;
                    scheduleSession.LastModificationTime = DateTime.Now;
                    exists = true;
                }
            }
            if (exists)
            {
                scheduleSession.RoomsSheet = message;
                scheduleSession.LastModificationTime = DateTime.Now;
                var answer = $"Хорошо, таблица найдена (или создана) \"{scheduleSession.RoomsSheet}\"." +
                    " Если вы еще не заполнили таблицы данными сделайте это. " +
                    "Как будете готовы, нажмите на кнопку \"Готово\"";

                // keyboard update
                var keyboard = CreateKeyboard(new List<string> { "Готово" }, 1);
                await client.SendTextMessageAsync(chatID, answer, ParseMode.Default, false, false, 0, keyboard);
            }
            else
            {
                var answer = "Не нашел такой таблицы. Попробуйте снова";

                // keyboard update
                await client.SendTextMessageAsync(chatID, answer);
            }
        }

        private async void HandleDataValidationAndAskForOutputSheetIfSuccess(long chatID, string message,
                AdditionalSessionState additionalSessionState)
        {
            if (message == "Готово" && !additionalSessionState.TableValidationInProgress)
            {
                var answer = "Сейчас проверю правильность введенных данных. Ожидайте.";
                await client.SendTextMessageAsync(chatID, answer);
                // Maybe async method t check and send message with report

                // Validation
                //var isValid, msg = ...
                var isValid = true;
                if (isValid)
                {
                    additionalSessionState.DataIsValid = true;
                    additionalSessionState.TableValidationInProgress = false;
                    var answer2 = "Все отлично. Выберите имя таблицы, где вывести расписание";

                    // keyboard update
                    repo.SetUpSheetInfo();
                    var sheetNames = repo.CurrentSheetInfo.Sheets.Keys.ToList();
                    sheetNames.Add("Создать");
                    var keyboard = CreateKeyboard(sheetNames, 6);
                    await client.SendTextMessageAsync(chatID, answer2, ParseMode.Default, false, false, 0, keyboard);
                }
                else
                {
                    // Error output
                    Console.WriteLine("Schedule error output");
                }
            }
            else
            {
                var answer = "Не понял. Нажмите \"Готово\", как закончите вводить данные.";
                await client.SendTextMessageAsync(chatID, answer);
            }
        }

        public async void HandleScheduleSheetAndCreateSchedule(long chatID, string message,
                ScheduleSession scheduleSession, AdditionalSessionState additionalSessionState)
        {
            var exists = false;
            // If specified "Create", create 
            if (message == "Создать")
            {
                repo.SetUpSheetInfo();
                var takenNames = repo.CurrentSheetInfo.Sheets.Keys.ToList();
                var newSheetName = FindUniqueName(takenNames, "Schedule");
                repo.CreateNewSheet(newSheetName);
                scheduleSession.ScheduleSheet = newSheetName;
                scheduleSession.LastModificationTime = DateTime.Now;
                exists = true;
            }
            // Check what this sheet really exists
            else
            {
                repo.SetUpSheetInfo();
                var takenNames = repo.CurrentSheetInfo.Sheets.Keys.ToList();
                if (takenNames.Contains(message))
                {
                    scheduleSession.ScheduleSheet = message;
                    scheduleSession.LastModificationTime = DateTime.Now;
                    exists = true;
                }
            }

            if (exists)
            {
                scheduleSession.ScheduleSheet = message;
                scheduleSession.LastModificationTime = DateTime.Now;
                var answer = $"Хорошо, таблица найдена (или создана) \"{scheduleSession.ScheduleSheet}\"." +
                    " Начинаю составление расписания. Напишу, когда будет готово";
                additionalSessionState.CreatingSchedule = true;
                // Save current session if the schedule is ready
                await client.SendTextMessageAsync(chatID, answer);
            }
            else
            {
                var answer = "Не нашел такой таблицы. Попробуйте снова.";
                // keyboard update
                await client.SendTextMessageAsync(chatID, answer);
            }
        }

        private string FindUniqueName(List<string> takenNames, string desiredName = "Schedule")
        {
            while (takenNames.Contains(desiredName))
            {
                desiredName += "I";
            }
            return desiredName;
        }

        private static ReplyKeyboardMarkup CreateKeyboard(IEnumerable<string> buttonNames, int maxButtonCountPerRow = 4)
        {
            var rowCount = buttonNames.Count() / maxButtonCountPerRow;
            if (buttonNames.Count() % maxButtonCountPerRow != 0)
            {
                rowCount++;
            }
            var buttons = new KeyboardButton[rowCount][];
            var currentRow = new KeyboardButton[Math.Min(buttonNames.Count(), maxButtonCountPerRow)];
            var currCount = 0;
            var currRow = 0;
            foreach (var buttonName in buttonNames)
            {
                currCount++;
                if (currCount - maxButtonCountPerRow * currRow > maxButtonCountPerRow)
                {
                    buttons[currRow] = currentRow;
                    currRow++;
                    currentRow = new KeyboardButton[Math.Min(buttonNames.Count() - maxButtonCountPerRow * currRow, maxButtonCountPerRow)]; ;
                    currentRow[0] = new KeyboardButton(buttonName);
                    continue;
                }
                currentRow[currCount - maxButtonCountPerRow * currRow - 1] = new KeyboardButton(buttonName);
            }
            if (currentRow.Length > 0)
            {
                buttons[currRow] = currentRow;
            }
            var replyKeyboardMarkup = new ReplyKeyboardMarkup();
            replyKeyboardMarkup.Keyboard = buttons;
            return replyKeyboardMarkup;
        }
    }

    public class AdditionalSessionState
    {
        public long ID { get; }
        public bool AccessRecieved;
        public bool TableValidationInProgress;
        public bool DataIsValid;
        public bool CreatingSchedule;

        public AdditionalSessionState(long id)
        {
            ID = id;
        }
    }
}
