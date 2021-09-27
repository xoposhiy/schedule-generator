using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

using Ninject;

using Infrastructure.FirebaseRepository;
using Infrastructure.GoogleSheetsRepository;
using Infrastructure.SheetPatterns;

namespace Application.TelegramBot
{
    public class TgBot
    {
        private StandardKernel container;
        private static readonly Regex LinkRegex = new Regex("https://docs.google.com/spreadsheets/d/([a-zA-Z0-9-_]+)");

        private readonly List<string> requisitionSheetHeaders;
        private readonly List<string> requirementsSheetHeaderComments;

        private readonly List<string> learningPlanSheetHeaders;
        private readonly List<string> learningPlanSheetHeaderComments;

        private List<(string pattern, string msg)> requisitionPatternMsgList;
        private List<(string pattern, string msg)> learningPlanPatternMsgList;

        private readonly SheetTableEvaluator requisitionEvaluator;
        private readonly SheetTableEvaluator learningPlanEvaluator;

        private readonly TelegramBotClient client;
        private readonly string repoSecret;

        //private GSRepository repo;
        private readonly SessionRepository sessionRepository;
        private readonly string credentialAddressToShare;
        private readonly Dictionary<long, ScheduleSession> sessionDict;
        private readonly Dictionary<long, AdditionalSessionState> additionalStateDict;
        private readonly Dictionary<long, GsRepository> repoDict;
        public TgBot(string token, string repoSecret, string firebaseSecret, string dbBasePath,
            List<string> requisitionSheetHeaders,
            List<string> requirementsSheetHeaderComments,
            List<string> learningPlanSheetHeaders,
            List<string> learningPlanSheetHeaderComments,
            List<(string pattern, string msg)> requisitionPatternMsgList,
            List<(string pattern, string msg)> learningPlanPatternMsgList,
            StandardKernel container)
        {
            client = new TelegramBotClient(token);
            sessionRepository = new SessionRepository(dbBasePath, firebaseSecret);
            sessionDict = new Dictionary<long, ScheduleSession>();
            additionalStateDict = new Dictionary<long, AdditionalSessionState>();
            repoDict = new Dictionary<long, GsRepository>();
            this.repoSecret = repoSecret;
            client.OnMessage += BotOnMessageReceived!;
            client.OnMessageEdited += BotOnMessageReceived!;

            var readedString = System.IO.File.ReadAllText(repoSecret);
            var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(readedString);
            credentialAddressToShare = values["client_email"];

            this.requisitionSheetHeaders = requisitionSheetHeaders;
            this.requirementsSheetHeaderComments = requirementsSheetHeaderComments;
            this.learningPlanSheetHeaders = learningPlanSheetHeaders;
            this.learningPlanSheetHeaderComments = learningPlanSheetHeaderComments;
            this.requisitionPatternMsgList = requisitionPatternMsgList;
            this.learningPlanPatternMsgList = learningPlanPatternMsgList;

            requisitionEvaluator = new SheetTableEvaluator(requisitionPatternMsgList);
            learningPlanEvaluator = new SheetTableEvaluator(learningPlanPatternMsgList);

            this.container = container;
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
            var chatId = message.Chat.Id;
            var isNew = false;
            if (!sessionDict.ContainsKey(chatId))
            {
                isNew = GetScheduleSessionByChatId(chatId, out var scheduleSession);
                sessionDict[chatId] = scheduleSession;
            }

            var currentSession = sessionDict[chatId];

            if (!additionalStateDict.ContainsKey(chatId))
            {
                isNew = GetAdditionalSessionStateByChatId(chatId, currentSession, out var additionalSessionState);
                additionalStateDict[chatId] = additionalSessionState;
            }
            var currentAdditionalState = additionalStateDict[chatId];

            GsRepository? repo = null;
            if (!string.IsNullOrEmpty(currentSession.SpreadsheetUrl) && currentAdditionalState.AccessReceived)
            {
                if (!repoDict.ContainsKey(chatId))
                {
                    repoDict[chatId] = new GsRepository("ScheduleGenerator", repoSecret, currentSession.SpreadsheetUrl);
                    repoDict[chatId].SetUpSheetInfo();
                }

                repo = repoDict[chatId];
            }

            if (message.Type == MessageType.Text)
            {
                if (message.Text == "/help" || message.Text == "Помощь")
                {
                    ShowHelp(chatId);
                }
                else if (message.Text == "/restart" || message.Text == "Заново")
                {
                    RestartSessionFor(chatId);
                }
                else if (string.IsNullOrEmpty(currentSession.SpreadsheetUrl))
                {
                    HandleSheetUrlAnswerAndAskForAccessIfSuccess(chatId, message.Text, currentSession, isNew);
                }
                else if (!currentAdditionalState.AccessReceived)
                {
                    CheckAccessAndAskForRequisitionSheetIfSuccess(chatId, message.Text, currentSession, currentAdditionalState);
                }
                else if (string.IsNullOrEmpty(currentSession.InputRequirementsSheet))
                {
                    HandleRequisitionSheetAndAskForLearningPlanIfSuccess(chatId, message.Text, currentSession, repo);
                }
                else if (string.IsNullOrEmpty(currentSession.LearningPlanSheet))
                {
                    HandleLearningPlanSheetAndAskForRoomSheetIfSuccess(chatId, message.Text, currentSession, repo);
                }
                else if (!currentAdditionalState.DataIsValid)
                {
                    HandleDataValidationAndAskForOutputSheetIfSuccess(chatId, message.Text, currentSession, currentAdditionalState, repo);
                }
                else if (string.IsNullOrEmpty(currentSession.ScheduleSheet))
                {
                    HandleScheduleSheetAndCreateSchedule(chatId, message.Text, currentSession, currentAdditionalState, repo);
                }
                else
                {
                    if (currentAdditionalState.CreatingSchedule)
                    {
                        var answer = "Составляю расписание. Ожидайте.";
                        await client.SendTextMessageAsync(chatId, answer);
                    }

                    // Save current session
                    sessionRepository.Save(chatId, currentSession);
                    await client.SendTextMessageAsync(chatId, "Кажется, предыдущая сессия уже завершиласть составлением расписания.\n" +
                        "Напишите \"Заново\" или /restart, чтобы начать сначала.");
                    currentAdditionalState.CreatingSchedule = false; // it will be also removed
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
                    additionalSessionState.AccessReceived = true;
                    isFirstTime = false;
                }
                catch
                {
                    //await client.SendTextMessageAsync(chatID, "Доступ не выдан. Попробуйте снова");
                    additionalSessionState.AccessReceived = false;
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
            var answer = "Я — Бот для составления расписания.\n Чтобы начать сначала введите \"Заново\" или /restart";
            await client.SendTextMessageAsync(chatID, answer, replyMarkup: new ReplyKeyboardRemove());
        }

        private async void RestartSessionFor(long chatID)
        {
            sessionDict.Remove(chatID);
            additionalStateDict.Remove(chatID);
            repoDict.Remove(chatID);
            sessionRepository.Delete(chatID);
            var answer = "Начинаем все сначала. Отправьте ссылку на гугл-таблицу с данными" +
                         " (url для таблицы в Google Sheets).";
            await client.SendTextMessageAsync(chatID, answer, replyMarkup: new ReplyKeyboardRemove());
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
                await client.SendTextMessageAsync(chatID, answer, replyMarkup: keyboard);
            }
            else
            {
                if (isChatFresh)
                {
                    var answer = "Привет. Я — бот для создания расписаний. Чтобы начать, отправьте " +
                        "ссылку на Spreadsheet (url для таблицы в Google Sheets)";
                    await client.SendTextMessageAsync(chatID, answer, replyMarkup: new ReplyKeyboardRemove());
                }
                else
                {
                    var answer = "Не понимаю. Сначала пришлите ссылку на Spreadsheet (url для таблицы" +
                        " в Google Sheets).";
                    await client.SendTextMessageAsync(chatID, answer, replyMarkup: new ReplyKeyboardRemove());
                }
            }
        }

        private async void CheckAccessAndAskForRequisitionSheetIfSuccess(long chatID, string message,
                ScheduleSession scheduleSession, AdditionalSessionState additionalSessionState)
        {
            if (message == "Готово")
            {

                GsRepository repo;
                // Access check
                try
                {
                    repo = new GsRepository("PsA32710i", repoSecret, scheduleSession.SpreadsheetUrl);
                }
                catch
                {
                    await client.SendTextMessageAsync(chatID, "Доступ не выдан. Попробуйте снова.");
                    return;
                }
                // If access accuired
                var answer = "Доступ получен. " +
                    "На каком листе таблицы будут пожелания по расписанию от преподавателей?";
                additionalSessionState.AccessReceived = true;
                // Buttons apiare

                repo.SetUpSheetInfo();
                var sheetNames = repo.CurrentSheetInfo!.Sheets.Keys.ToList();
                sheetNames.Add("Создать");
                var keyboard = CreateKeyboard(sheetNames, 6);

                await client.SendTextMessageAsync(chatID, answer, replyMarkup: keyboard);
            }
            else
            {
                var answer = "Дайте мне доступ на редактирование. Добавьте мой адрес в редакторы вашей таблицы.\n" +
                    $"Вот мой адрес: {credentialAddressToShare}";
                var keyboard = CreateKeyboard(new List<string> { "Готово" }, 1);
                await client.SendTextMessageAsync(chatID, answer, replyMarkup: keyboard);
            }
        }

        public async void HandleRequisitionSheetAndAskForLearningPlanIfSuccess(long chatID, string message,
                ScheduleSession scheduleSession, GsRepository repo)
        {
            var exists = false;
            // If specified "Create", create 
            if (message == "Создать")
            {
                repo.SetUpSheetInfo();
                var takenNames = repo.CurrentSheetInfo.Sheets.Keys.ToList();
                var newSheetName = FindUniqueName(takenNames, "Requisition");
                repo.CreateNewSheet(newSheetName);
                repo.SetUpSheetInfo();
                // Add headers
                HeaderPatternCreator.SetUpHeaders(repo, newSheetName, (0, 0), requisitionSheetHeaders, requirementsSheetHeaderComments);

                scheduleSession.InputRequirementsSheet = newSheetName;
                scheduleSession.LastModificationTime = DateTime.Now;
                exists = true;
            }
            // Check if this sheet really exists
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

                var answer = $"Хорошо, лист \"{scheduleSession.InputRequirementsSheet}\" найден/создан." +
                    $" Теперь укажите название листа с учебным планом";
                // keyboard update
                repo.SetUpSheetInfo();
                var sheetNames = repo.CurrentSheetInfo.Sheets.Keys.ToList();
                sheetNames.Add("Создать");
                var keyboard = CreateKeyboard(sheetNames, 6);

                await client.SendTextMessageAsync(chatID, answer, replyMarkup: keyboard);
            }
            else
            {
                var answer = "Не нашел такой таблицы. Попробуйте снова.";
                await client.SendTextMessageAsync(chatID, answer);
            }
        }

        public async void HandleLearningPlanSheetAndAskForRoomSheetIfSuccess(long chatID, string message,
                ScheduleSession scheduleSession, GsRepository repo)
        {
            var exists = false;
            // If specified "Create", create 
            if (message == "Создать")
            {
                repo.SetUpSheetInfo();
                var takenNames = repo.CurrentSheetInfo.Sheets.Keys.ToList();
                var newSheetName = FindUniqueName(takenNames, "LearningPlan");
                repo.CreateNewSheet(newSheetName);
                repo.SetUpSheetInfo();
                // Add headers
                HeaderPatternCreator.SetUpHeaders(repo, newSheetName, (0, 0), learningPlanSheetHeaders, learningPlanSheetHeaderComments);

                scheduleSession.LearningPlanSheet = newSheetName;
                scheduleSession.LastModificationTime = DateTime.Now;
                exists = true;
            }
            // Check if this sheet really exists
            else
            {
                repo.SetUpSheetInfo();
                var takenNames = repo.CurrentSheetInfo.Sheets.Keys.ToList();
                if (takenNames.Contains(message))
                {
                    scheduleSession.LearningPlanSheet = message;
                    scheduleSession.LastModificationTime = DateTime.Now;
                    exists = true;
                }
            }
            if (exists)
            {
                scheduleSession.LastModificationTime = DateTime.Now;
                var answer = $"Хорошо, лист \"{scheduleSession.LearningPlanSheet}\" найден/создан." +
                    " Если вы еще не заполнили выбранные листы необходимыми данными, сделайте это. " +
                    "Как будете готовы, нажмите на кнопку \"Готово\"";

                // keyboard update
                var keyboard = CreateKeyboard(new List<string> { "Готово" }, 1);
                await client.SendTextMessageAsync(chatID, answer, replyMarkup: keyboard);
            }
            else
            {
                var answer = "Не нашел такой таблицы. Попробуйте снова";

                // keyboard update
                await client.SendTextMessageAsync(chatID, answer);
            }
        }

        private async void HandleDataValidationAndAskForOutputSheetIfSuccess(long chatID, string message, ScheduleSession scheduleSession,
                AdditionalSessionState additionalSessionState, GsRepository repo)
        {
            if (message == "Готово" && !additionalSessionState.TableValidationInProgress)
            {
                var answer = "Проверяю корректность введенных данных. Ожидайте.";
                await client.SendTextMessageAsync(chatID, answer);
                // Maybe async method t check and send message with report
                var isValid = true;

                // REQUISITION EVALUATION
                Console.WriteLine("REQUISITION EVALUATION");
                // read data
                var requisitionData = SheetTableReader.ReadRowsFromSheet(
                    repo, scheduleSession.InputRequirementsSheet, (0, 0), requisitionSheetHeaders.Count);
                // clear last errors
                if (additionalSessionState.RequisitionLastErrorCoords != null && additionalSessionState.RequisitionLastErrorCoords.Any())
                {
                    SheetTableErrorPainter.ClearErrorPaint(repo, scheduleSession.InputRequirementsSheet, (0, 0),
                        additionalSessionState.RequisitionLastErrorCoords);
                }
                // get list of errors
                var requisitionErrors = requisitionEvaluator.Evaluate(requisitionData, (1, 0));
                if (requisitionErrors.Any())
                {
                    isValid = false;
                    // save errors to additional info
                    additionalSessionState.RequisitionLastErrorCoords = requisitionErrors.Select(x => x.Item1).ToList();
                    // paint errors
                    SheetTableErrorPainter.PaintErrors(repo, scheduleSession.InputRequirementsSheet, (0, 0), requisitionErrors);
                }

                // LEARNING PLAN EVALUATION
                Console.WriteLine("LEARNING PLAN EVALUATION");
                // read data
                var learningPlanData = SheetTableReader.ReadRowsFromSheet(
                    repo, scheduleSession.LearningPlanSheet, (0, 0), learningPlanSheetHeaders.Count);
                // clear last errors
                if (additionalSessionState.LearningPlanLastErrorCoords != null && additionalSessionState.LearningPlanLastErrorCoords.Any())
                {
                    SheetTableErrorPainter.ClearErrorPaint(repo, scheduleSession.LearningPlanSheet, (0, 0),
                        additionalSessionState.LearningPlanLastErrorCoords);
                }
                // get list of errors
                var learningPlanErrors = learningPlanEvaluator.Evaluate(learningPlanData, (1, 0));
                if (learningPlanErrors.Any())
                {
                    isValid = false;
                    // save errors to additional info
                    additionalSessionState.LearningPlanLastErrorCoords = learningPlanErrors.Select(x => x.Item1).ToList();
                    // paint errors
                    SheetTableErrorPainter.PaintErrors(repo, scheduleSession.LearningPlanSheet, (0, 0), learningPlanErrors);
                }


                // Validation end

                if (isValid)
                {
                    additionalSessionState.DataIsValid = true;
                    additionalSessionState.TableValidationInProgress = false;
                    var answer2 = "Все отлично. Выберите название листа, где будет выведено расписание";

                    // keyboard update
                    repo.SetUpSheetInfo();
                    var sheetNames = repo.CurrentSheetInfo.Sheets.Keys.ToList();
                    sheetNames.Add("Создать");
                    var keyboard = CreateKeyboard(sheetNames, 6);
                    await client.SendTextMessageAsync(chatID, answer2, replyMarkup: keyboard);
                }
                else
                {
                    var errorMsgParts = new List<string>();
                    errorMsgParts.Add("Обнаружены ошибки. Выделены таблицах красным цветом.");
                    errorMsgParts.Add("Подробности в комменараниях ячеек, обратите внимание на формат указанный в заголоках.");
                    errorMsgParts.Add("Исправьте данные в следующих листах:");
                    if (requisitionErrors.Count > 0)
                    {
                        errorMsgParts.Add(scheduleSession.InputRequirementsSheet);
                    }
                    if (learningPlanErrors.Count > 0)
                    {
                        errorMsgParts.Add(scheduleSession.LearningPlanSheet);
                    }

                    var errorMsg = string.Join("\n", errorMsgParts);

                    // Error output
                    var keyboard = CreateKeyboard(new List<string> { "Готово" }, 1);
                    await client.SendTextMessageAsync(chatID, errorMsg, replyMarkup: keyboard);

                    Console.WriteLine("Schedule error output");
                }
            }
            else
            {
                var answer = "Не понял. Вам нужно нажать \"Готово\", как только закончите вводить данные, " +
                             "чтобы начать составлять расписание.";
                var keyboard = CreateKeyboard(new List<string> { "Готово" }, 1);
                await client.SendTextMessageAsync(chatID, answer, replyMarkup: keyboard);
            }
        }

        public async void HandleScheduleSheetAndCreateSchedule(long chatID, string message,
                ScheduleSession scheduleSession, AdditionalSessionState additionalSessionState, GsRepository repo)
        {
            var exists = false;
            var sheetName = message;
            // If specified "Create", create 
            if (message == "Создать")
            {
                repo.SetUpSheetInfo();
                var takenNames = repo.CurrentSheetInfo.Sheets.Keys.ToList();
                var newSheetName = FindUniqueName(takenNames);
                sheetName = newSheetName;
                repo.CreateNewSheet(newSheetName);
                repo.SetUpSheetInfo();

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
                scheduleSession.ScheduleSheet = sheetName;
                scheduleSession.LastModificationTime = DateTime.Now;
                var answer = $"Хорошо, лист \"{scheduleSession.ScheduleSheet}\" найден/создан." +
                    " Начинаю составление расписания. Напишу, когда будет готово";
                additionalSessionState.CreatingSchedule = true;

                await client.SendTextMessageAsync(chatID, answer, replyMarkup: new ReplyKeyboardRemove());

                // SCHEDULE GENERATION
                // var (requisitions, learningPlan, classrooms) = SheetToRequisitionConverter.ConvertToRequisitions(
                //     repo, scheduleSession.InputRequirementsSheet, scheduleSession.LearningPlanSheet);
                //
                // var evaluator = container.Get<MeetingEvaluator>();
                //
                // var requisition = new Requisition(requisitions.ToArray());
                //
                // var schedule = new GreedyScheduleGenerator().MakeSchedule(learningPlan, evaluator, requisition);
                //
                // var converter = new ScheduleSpreadsheetConverter(repo, scheduleSession.ScheduleSheet);
                // converter.Build(schedule);
                
                // TODO починить тг-бота

                answer = $"Расписание готово (НЕТ)! Оно находится в листе (НЕТ) \"{scheduleSession.ScheduleSheet}\"." +
                         " Напишите \"/restart\", если хотите составить еще одно расписание.";
                await client.SendTextMessageAsync(chatID, answer, replyMarkup: new ReplyKeyboardRemove());

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


        //public async void StartSolvingAndNotifyWhenDone(long chatID, ScheduleSession session, GSRepository repo)
        //{
        //    var requisitions = SheetToRequisitionConverter.ConvertToRequisitions(
        //        repo, session.InputRequirementsSheet, session.LearningPlanSheet);
        //}
    }

    public class AdditionalSessionState
    {
        public long ID { get; }
        public bool AccessReceived;
        public bool TableValidationInProgress;
        public bool DataIsValid;
        public bool CreatingSchedule;

        public IList<(int, int)> RequisitionLastErrorCoords;
        public IList<(int, int)> LearningPlanLastErrorCoords;

        public AdditionalSessionState(long id)
        {
            ID = id;
            RequisitionLastErrorCoords = new List<(int, int)>();
            LearningPlanLastErrorCoords = new List<(int, int)>();
        }
    }
}
