using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;


using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using GoogleSheetsRepository;

namespace Bot
{
    public class TBot
    {
        private TelegramBotClient client;
        private string repoSecret;
        private Dictionary<Regex, Func<string, string>> preparedBehavior;
        private Dictionary<long, string> ChatIdSheetLinkDict;
        private Regex LinkRegex = new Regex("https://docs.google.com/spreadsheets/d/([a-zA-Z0-9-_]+)");
        private States State;
        private string link;
        private string sheetId;
        private GSRepository repo;
        private bool GotAccess;
        private string RequirementsSheet;
        private string ScheduleSheet;
        public TBot(string token, string repoSecret)
        {
            client = new TelegramBotClient(token);
            this.repoSecret = repoSecret;
            preparedBehavior = new Dictionary<Regex, Func<string, string>>();
            ChatIdSheetLinkDict = new Dictionary<long, string>();
            client.OnMessage += BotOnMessageReceived;
            client.OnMessageEdited += BotOnMessageReceived;
            SetUpCommands();
            State = States.NeedLink;
        }

        public void Start()
        {
            client.StartReceiving();
        }

        public void Stop()
        {
            client.StartReceiving();
        }

        private void SetUpCommands()
        {
            preparedBehavior[new Regex("https://docs.google.com/spreadsheets/.*")] = (addr) => addr + "!";
        }

        private async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;
            //switch (message?.Type) {
            //    case MessageType.Text:
            //        switch (State)
            //        break;
            //}
            if (message?.Type == MessageType.Text)
            {
                if (message.Text == "/help")
                {
                    var answer = "Im the Schedule Generator Bot. To start schedule creation send Spread Sheet link. Copy you Google Sheet url and send me =)";
                    await client.SendTextMessageAsync(message.Chat.Id, answer);
                    return;
                }
                if (message.Text == "/restart")
                {
                    await client.SendTextMessageAsync(message.Chat.Id, "Restarted");
                    State = States.NeedLink;
                    link = null;
                    sheetId = null;
                    return;
                }
                if (State == States.NeedLink)
                {
                    var isLink = LinkRegex.IsMatch(message.Text);
                    // Validate
                    if (!isLink)
                    {
                        await client.SendTextMessageAsync(message.Chat.Id, "Invalid Link. Try send GoogleSheets Spread sheet link again.");
                        return;
                    }
                    //
                    link = message.Text;
                    sheetId = LinkRegex.Match(link).Groups[1].Value;
                    Console.WriteLine("Got Link {0} -> {1}", link, sheetId);
                    
                    var answer = "Give me access. Add me to your Google Sheet. Click Done when you give me access ";
                    State = States.NeedAccess;
                    var replyKeyboardMarkup = new ReplyKeyboardMarkup();
                    //replyKeyboardMarkup.
                    replyKeyboardMarkup.Keyboard = new KeyboardButton[][]
                    {
                        new KeyboardButton[]
                        {
                            new KeyboardButton("Done")
                        }
                    };

                    await client.SendTextMessageAsync(message.Chat.Id, answer, ParseMode.Default, false, false, 0, replyKeyboardMarkup);

                    

                    return;
                }
                if (State == States.NeedAccess)
                {
                    if (message.Text != "Done") {
                        await client.SendTextMessageAsync(message.Chat.Id, $"Click Done when you give me access");
                        return;
                    }
                    // Check Access
                    GotAccess = false;
                    try
                    {
                        if (repo is null)
                        {
                            repo = new GSRepository("Bot", repoSecret, link);
                        }
                        else
                        {
                            repo.ChangeTable(link);
                        }
                    }
                    catch
                    {
                        await client.SendTextMessageAsync(message.Chat.Id, $"Give me access to {link}");
                        return;
                    }
                    GotAccess = true;
                    State = States.NeedRequirmentsSheet;
                }
                if (State == States.NeedRequirmentsSheet)
                {
                    var answer = "On which sheet of the table will there be scheduled wishes from teachers?";
                    State = States.ScheduleCreation;
                    var replyKeyboardMarkup = new ReplyKeyboardMarkup();
                    var sheetNames = repo.CurrentSheetInfo.Sheets.Keys.ToList();
                    sheetNames.Add("Create New");
                    replyKeyboardMarkup.Keyboard = sheetNames.Select(name => new KeyboardButton[] { new KeyboardButton(name) });


                    await client.SendTextMessageAsync(message.Chat.Id, answer, ParseMode.Default, false, false, 0, replyKeyboardMarkup);
                    return;
                }
                if (State == States.ScheduleCreation)
                {
                    var isValid = false;
                    if (message.Text == "Create New") {
                        // Check if name exists
                        var uniqueSheetName = FindUniqueName(repo.CurrentSheetInfo.Sheets.Keys.ToList());
                        repo.CreateNewSheet(uniqueSheetName);
                        RequirementsSheet = uniqueSheetName;
                        isValid = true;
                    }
                    if (!isValid && !repo.CurrentSheetInfo.Sheets.ContainsKey(message.Text)) {
                        RequirementsSheet = message.Text;
                        await client.SendTextMessageAsync(message.Chat.Id, "Invalid Sheet name. Choose from keyboard button.");
                        return;
                    }

                    // Creating Schedule
                    // Validation
                    var answer = "There are no errors in the input requirements! On which sheet of the table should the schedule be displayed?";
                    State = States.NeedOutputSheet;
                    var replyKeyboardMarkup = new ReplyKeyboardMarkup();
                    var sheetNames = repo.CurrentSheetInfo.Sheets.Keys.ToList();
                    sheetNames.Add("Create New");
                    replyKeyboardMarkup.Keyboard = sheetNames.Select(name => new KeyboardButton[] { new KeyboardButton(name) });

                    await client.SendTextMessageAsync(message.Chat.Id, answer, ParseMode.Default, false, false, 0, replyKeyboardMarkup);
                    return;
                }

                if (State == States.ScheduleCreation)
                {
                    var isValid = false;
                    if (message.Text == "Create New")
                    {
                        // Check if name exists
                        var uniqueSheetName = FindUniqueName(repo.CurrentSheetInfo.Sheets.Keys.ToList());
                        repo.CreateNewSheet(uniqueSheetName);
                        RequirementsSheet = uniqueSheetName;
                        isValid = true;
                    }
                    if (!isValid && !repo.CurrentSheetInfo.Sheets.ContainsKey(message.Text))
                    {
                        RequirementsSheet = message.Text;
                        await client.SendTextMessageAsync(message.Chat.Id, "Invalid Sheet name. Choose from keyboard button.");
                        return;
                    }
                    State = States.NeedLink;
                    await client.SendTextMessageAsync(message.Chat.Id, "Ok");
                }

                await client.SendTextMessageAsync(message.Chat.Id, "What? Type /help");
            }
        }

        private string FindUniqueName(List<string> takenNames, string desiredName="Schedule") {
            while (takenNames.Contains(desiredName)) {
                desiredName += "I";
            }
            return desiredName;
        }
    }

    public class Scenario
    {
        public ScenarioStep Start { get; set; }
        public ScenarioStep Current { get; set; }
        public ScenarioStep Finish { get; set; }
        public long ChatId { get; set; }
    }

    public class ScenarioStep
    {
        public string Description { get; set; }
        public Regex Pattern { get; set; }
        public OnErrorState OnErrorWay { get; set; }
        public Action<TelegramBotClient, long> OnErrorAction;
        public Func<string, TelegramBotClient, long, bool> React { get; set; }
        public TelegramBotClient client { get; set; }
    }


    public enum OnErrorState
    {
        Fatal,
        Repeat,
        TryAgain
    }

    public enum States
    {
        None,
        NeedLink,
        NeedAccess,
        NeedRequirmentsSheet,
        ScheduleCreation,
        NeedOutputSheet,
    }

    public class ConversationPoint
    {
        public string Description { get; set; }
        public Regex ExpectedAnswer { get; set; }
        public ConversationPoint PrevConvPoint { get; set; }
        public ConversationPoint NextConvPoint { get; set; }
        public Func<string, TelegramBotClient, string> Reaction { get; set; }

        public ConversationPoint(string description, string pattern, Func<string, TelegramBotClient, string> reaction)
        {
            Description = description;
            ExpectedAnswer = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            Reaction = reaction;
        }

        public bool CheckAnswer(string answer)
        {
            return ExpectedAnswer.IsMatch(answer);
        }

        public string React(string answer, TelegramBotClient client)
        {
            return Reaction(answer, client);
        }
    }

    public class Conversation
    {

    }
}
