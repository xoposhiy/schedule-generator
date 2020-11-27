using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using Newtonsoft.Json;

using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using GoogleSheetsRepository;
using FirebaseRepository;

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
                // Ask Firebase
                //   if found check access to spreadsheet and set what access already recieved
                var requestedSession = sessionRepository.Get(chatID);
                if (requestedSession != null)
                {
                    sessionDict[chatID] = requestedSession;
                }
                else
                {
                    // If not found in db
                    sessionDict[chatID] = new ScheduleSession();
                    sessionDict[chatID].Id = chatID;
                    sessionDict[chatID].LastModificationInitiator = "TelegramBot";
                    sessionDict[chatID].LastModificationTime = DateTime.Now;
                    sessionDict[chatID].DialogState = DialogState.Initial;
                    isNew = true;
                }
            }
            var currentSession = sessionDict[chatID];


            if (!additionalStateDict.ContainsKey(chatID))
            {
                additionalStateDict[chatID] = new AdditionalSessionState(chatID);
                isNew = true;
                if (!string.IsNullOrEmpty(currentSession.InputRequirementsSheet))
                {
                    try
                    {
                        repo = new GSRepository("PsA32710i", repoSecret, currentSession.SpreadsheetUrl);
                        additionalStateDict[chatID].AccessRecieved = true;
                        isNew = false;
                    }
                    catch (Exception e)
                    {
                        await client.SendTextMessageAsync(chatID, "Доступ не выдан. Попробуйте снова");
                        additionalStateDict[chatID].AccessRecieved = false;
                    }
                    if (!string.IsNullOrEmpty(currentSession.ScheduleSheet))
                    {
                        additionalStateDict[chatID].DataIsValid = true;
                    }
                }
            }
            var currentAdditionalState = additionalStateDict[chatID];


            if (message?.Type == MessageType.Text)
            {
                if (message.Text == "/help" || message.Text == "Помощь")
                {
                    var answer = "Я Бот для составления расписания";
                    await client.SendTextMessageAsync(chatID, answer);
                }
                else if (message.Text == "/restart" || message.Text == "Заново")
                {
                    sessionDict.Remove(chatID);
                    additionalStateDict.Remove(chatID);
                    sessionRepository.Delete(chatID);
                    var answer = "Начинаем все сначала";
                    await client.SendTextMessageAsync(chatID, answer);
                }
                else if (string.IsNullOrEmpty(currentSession.SpreadsheetUrl))
                {
                    if (LinkRegex.IsMatch(message.Text))
                    {
                        currentSession.SpreadsheetUrl = message.Text;
                        currentSession.LastModificationTime = DateTime.Now;
                        var answer = "URL получен. Дайте мне доступ на редактирование. Добавьте мой адрес в редакторы вашей таблицы.\n";
                        answer += $"Нажмите на кнопку \"Готово\" когда сделаете.\n";
                        var keyboard = CreateKeyboard(new List<string> { "Готово" }, 1);

                        answer += $"Вот мой адрес: {credentialAddressToShare}";
                        await client.SendTextMessageAsync(chatID, answer, ParseMode.Default, false, false, 0, keyboard);
                    }
                    else
                    {
                        if (isNew)
                        {
                            var answer = "Привет. Я бот для создания расписаний. Чтобы начать киньте ссылку на Spreadsheet (url для таблицы в Google Sheets)";
                            await client.SendTextMessageAsync(chatID, answer);
                        }
                        else
                        {
                            var answer = "Не осознал. Сначала скиньте ссылку на Spreadsheet (url для таблицы в Google Sheets). Просто отправьте суда текстом.";
                            await client.SendTextMessageAsync(chatID, answer);
                        }
                    }
                }
                else if (!currentAdditionalState.AccessRecieved)
                {
                    if (message.Text == "Готово")
                    {
                        // Access check
                        try
                        {
                            repo = new GSRepository("PsA32710i", repoSecret, currentSession.SpreadsheetUrl);
                        }
                        catch (Exception e)
                        {
                            await client.SendTextMessageAsync(chatID, "Доступ не выдан. Попробуйте снова");
                            return;
                        }
                        // If access accuired
                        var answer = "Доступ получен. " +
                            "На каком листе таблицы будут пожелания по расписанию от преподавателей?";
                        currentAdditionalState.AccessRecieved = true;
                        // Buttons apiare

                        repo.SetUpSheetInfo();
                        var sheetNames = repo.CurrentSheetInfo.Sheets.Keys.ToList();
                        sheetNames.Add("Создать");
                        var keyboard = CreateKeyboard(sheetNames, 3);

                        await client.SendTextMessageAsync(chatID, answer, ParseMode.Default, false, false, 0, keyboard);
                    }
                    else
                    {
                        var answer = "Дайте мне доступ на редактирование. Добавьте мой адрес в редакторы вашей таблицы.\n" +
                            $"Вот мой адрес: {credentialAddressToShare}";
                        await client.SendTextMessageAsync(chatID, answer);
                    }
                }
                else if (string.IsNullOrEmpty(currentSession.InputRequirementsSheet))
                {
                    var exists = false;
                    // If specified "Create", create 
                    if (message.Text == "Создать")
                    {
                        repo.SetUpSheetInfo();
                        var takenNames = repo.CurrentSheetInfo.Sheets.Keys.ToList();
                        var newSheetName = FindUniqueName(takenNames, "Requestion");
                        repo.CreateNewSheet(newSheetName);
                        currentSession.InputRequirementsSheet = newSheetName;
                        currentSession.LastModificationTime = DateTime.Now;
                        exists = true;
                    }
                    // Check what this sheet really exists
                    else
                    {
                        repo.SetUpSheetInfo();
                        var takenNames = repo.CurrentSheetInfo.Sheets.Keys.ToList();
                        if (takenNames.Contains(message.Text))
                        {
                            currentSession.InputRequirementsSheet = message.Text;
                            currentSession.LastModificationTime = DateTime.Now;
                            exists = true;
                        }
                    }

                    if (exists)
                    {

                        var answer = $"Хорошо, таблица найдена (или создана) \"{currentSession.InputRequirementsSheet}\". Теперь укажите имя таблицы с учебным планом";
                        // keyboard update
                        repo.SetUpSheetInfo();
                        var sheetNames = repo.CurrentSheetInfo.Sheets.Keys.ToList();
                        sheetNames.Add("Создать");
                        var keyboard = CreateKeyboard(sheetNames, 4);

                        await client.SendTextMessageAsync(chatID, answer, ParseMode.Default, false, false, 0, keyboard);
                    }
                    else
                    {
                        var answer = "Не нашел такой таблицы. Попробуйте снова";
                        await client.SendTextMessageAsync(chatID, answer);
                    }
                }
                else if (string.IsNullOrEmpty(currentSession.RoomsSheet))
                {
                    var exists = false;
                    // If specified "Create", create 
                    if (message.Text == "Создать")
                    {
                        repo.SetUpSheetInfo();
                        var takenNames = repo.CurrentSheetInfo.Sheets.Keys.ToList();
                        var newSheetName = FindUniqueName(takenNames, "LearningPlan");
                        repo.CreateNewSheet(newSheetName);
                        currentSession.RoomsSheet = newSheetName;
                        currentSession.LastModificationTime = DateTime.Now;
                        exists = true;
                    }
                    // Check what this sheet really exists
                    else
                    {
                        repo.SetUpSheetInfo();
                        var takenNames = repo.CurrentSheetInfo.Sheets.Keys.ToList();
                        if (takenNames.Contains(message.Text))
                        {
                            currentSession.RoomsSheet = message.Text;
                            currentSession.LastModificationTime = DateTime.Now;
                            exists = true;
                        }
                    }
                    if (exists)
                    {
                        currentSession.RoomsSheet = message.Text;
                        currentSession.LastModificationTime = DateTime.Now;
                        var answer = $"Хорошо, таблица найдена (или создана) \"{currentSession.RoomsSheet}\". Если вы еще не заполнили таблицы данными сделайте это. " +
                            "Как будете готовы нажмите на кнопку \"Готово\"";

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
                else if (!currentAdditionalState.DataIsValid)
                {
                    if (message.Text == "Готово" && !currentAdditionalState.TableValidationInProgress)
                    {
                        var answer = "Сейчас проверю правильность введенных данных. Ожидайте.";
                        await client.SendTextMessageAsync(chatID, answer);
                        // Maybe async method t check and send message with report

                        // Validation
                        //var isValid, msg = ...
                        var isValid = true;
                        if (isValid)
                        {
                            currentAdditionalState.DataIsValid = true;
                            currentAdditionalState.TableValidationInProgress = false;
                            var answer2 = "Все отлично. Выберите имя таблицы, где вывести расписание";

                            // keyboard update
                            repo.SetUpSheetInfo();
                            var sheetNames = repo.CurrentSheetInfo.Sheets.Keys.ToList();
                            sheetNames.Add("Создать");
                            var keyboard = CreateKeyboard(sheetNames, 4);
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
                        var answer = "Не понял. Нажмите \"Готово\" как закончите вводить данные.";
                        await client.SendTextMessageAsync(chatID, answer);
                    }
                }
                else if (string.IsNullOrEmpty(currentSession.ScheduleSheet))
                {
                    var exists = false;
                    // If specified "Create", create 
                    if (message.Text == "Создать")
                    {
                        repo.SetUpSheetInfo();
                        var takenNames = repo.CurrentSheetInfo.Sheets.Keys.ToList();
                        var newSheetName = FindUniqueName(takenNames, "Schedule");
                        repo.CreateNewSheet(newSheetName);
                        currentSession.ScheduleSheet = newSheetName;
                        currentSession.LastModificationTime = DateTime.Now;
                        exists = true;
                    }
                    // Check what this sheet really exists
                    else
                    {
                        repo.SetUpSheetInfo();
                        var takenNames = repo.CurrentSheetInfo.Sheets.Keys.ToList();
                        if (takenNames.Contains(message.Text))
                        {
                            currentSession.ScheduleSheet = message.Text;
                            currentSession.LastModificationTime = DateTime.Now;
                            exists = true;
                        }
                    }

                    if (exists)
                    {
                        currentSession.ScheduleSheet = message.Text;
                        currentSession.LastModificationTime = DateTime.Now;
                        var answer = $"Хорошо, таблица найдена (или создана) \"{currentSession.ScheduleSheet}\". Начинаю составление расписания. Напишу, когда будет готово";
                        currentAdditionalState.CreatingSchedule = true;
                        // Save current session if the schedule is ready
                        await client.SendTextMessageAsync(chatID, answer);
                    }
                    else
                    {
                        var answer = "Не нашел такой таблицы. Попробуйте снова";
                        // keyboard update
                        await client.SendTextMessageAsync(chatID, answer);
                    }
                }
                else
                {
                    if (currentAdditionalState.CreatingSchedule)
                    {
                        var answer = "Составляю расписание. Ожидайте";
                        await client.SendTextMessageAsync(chatID, answer);
                    }

                    // Save current session
                    sessionRepository.Save(chatID, currentSession);
                    await client.SendTextMessageAsync(chatID, "Кажется что предыдущая сессия уже заверщиласть составлением расписания.\n" +
                        "Напишите \"Заново\" или /restart чтоюы начать сначала.");
                }
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
