using System;
using System.Text;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Timers;
using System.Threading.Tasks;
using Threepio.Translator;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using GameServerInfo;
using Threepio.Server;
using AIMLbot;

namespace Threepio.GameInterface
{
    public class AcademyGameConsole
    {
        private TranslatorService translatorService;
        private GameServer gameServer;
        private ServerManagement serverManager;

        private List<string> Players { get; set; }
        private string TargetedPlayer { get; set; }
        private bool IsSendingMessage { get; set; }
        private static string playerName = "-[KR]-Zabuza*:";
        private static bool isMasterOnly = false;

        //GetWindow WINAPI constants
        private const int GW_HWNDFIRST = 0;
        private const int GW_HWNDNEXT = 2;
        private const int GW_CHILD = 5;

        //SendMessage  & PostMessage constants
        private const int WM_SETTEXT = 0x000C;
        private const int WM_GETTEXT = 0x000D;
        private const int WM_GETTEXTLENGTH = 0x000E;
        private const int WM_KEYDOWN = 0x0100;
        private const int VK_RETURN = 0x0D;

        private IntPtr sendHandle;
        private IntPtr retrieveHandle;

        private string newText = "";
        private string oldText = "";

        private bool isFirstBatchOfMessages;

        public static Bot myBot;
        public static User myUser;

        private string user = "-[KR]-" + ConfigurationManager.AppSettings["PlayerName"] + ":";

        private const string windowName = "Jedi Knight Academy MP Console"; // Process' window name. Used to grab console (edit) handle
        private const string editClassName = "Edit"; // The handle we want to find.

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

        [DllImport("user32", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        static extern IntPtr GetWindow(IntPtr hwnd, int wFlag);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder className, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hwnd, int msg, IntPtr wParam, [MarshalAs(UnmanagedType.LPStr)] string lParam);

        [DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
        static extern int GetTextFromHandle(IntPtr hwndControl, uint Msg, int wParam, StringBuilder strBuffer);

        [DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
        static extern int GetTextLength(IntPtr hwndControl, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        public AcademyGameConsole()
        {
            serverManager = new ServerManagement(gameServer);

            Players = serverManager.GetPlayers();
            TargetedPlayer = "";

            isFirstBatchOfMessages = true;
            FindJediAcademyConsoleHandle();
        }

        private void FindJediAcademyConsoleHandle()
        {
            IntPtr consoleWindow = FindWindowByCaption(IntPtr.Zero, windowName);
            IntPtr childWindow = GetWindow(consoleWindow, GW_CHILD);

            StringBuilder classNameBuilder = new StringBuilder(256);

            while (childWindow != IntPtr.Zero)
            {
                GetClassName(childWindow, classNameBuilder, 79);
                if (classNameBuilder.ToString().Equals(editClassName))
                {
                    if (sendHandle == IntPtr.Zero)
                    {
                        sendHandle = childWindow;
                    }
                    else
                    {
                        myBot = new Bot();
                        myBot.loadSettings();
                        myUser = new User("consoleUser", myBot);
                        myBot.isAcceptingUserInput = false;
                        myBot.loadAIMLFromFiles();
                        myBot.isAcceptingUserInput = true;

                        retrieveHandle = childWindow;
                        TimerControl();

                        translatorService = new TranslatorService();
                        break;
                    }
                }
                childWindow = GetWindow(childWindow, GW_HWNDNEXT);
            }
        }

        /// <summary>
        /// Determines how long to wait for the next chat message.
        /// </summary>
        private void TimerControl()
        {
            Timer chatTimer = new Timer();
            chatTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent_Chat);
            chatTimer.Interval = 1;
            chatTimer.Enabled = true;

            //Timer playerTimer = new Timer();
            //playerTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent_Player);
            //playerTimer.Interval = 180000;
            //playerTimer.Enabled = true;
        }

        /// <summary>
        /// Retrieves chat messages every second.
        /// </summary>
        private void OnTimedEvent_Chat(object source, ElapsedEventArgs e)
        {
            GetChatMessages();
        }

        /// <summary>
        /// Retrieves chat messages every second.
        /// </summary>
        //private void OnTimedEvent_Player(object source, ElapsedEventArgs e)
        //{
        //    serverManager = new ServerManagement(gameServer);
        //    Players = serverManager.GetPlayers();

        //    MessageToConsole("Threepio", string.Format("Updating player list..."));
        //}

        private void GetChatMessages()
        {
            oldText = newText;

            var current = GetTextLength(retrieveHandle, WM_GETTEXTLENGTH, 0, 0);
            var sb = new StringBuilder(current);

            GetTextFromHandle(retrieveHandle, WM_GETTEXT, current + 1, sb);
            newText = sb.ToString();

            if (oldText.Length < newText.Length)
            {
                var index = newText.IndexOf(oldText);

                var cleanText = (index < 0) ? newText : newText.Remove(index, oldText.Length);

                CheckMessageForCommand(cleanText);
            }
        }

        /// <summary>
        /// Checks the given chat entry for user commands.
        /// </summary>
        /// <param name="command">The chat being analyzed.</param>
        private void CheckMessageForCommand(string command)
        {
            if (IsSendingMessage)
                return;

            //TODO: Refactor the string handling.
            var translateInstruction = ">";
            var stopTranslatingInstruction = ">>";

            string instruction1 = "_t";
            string botInstruction = "_3";
            string testCommand = "_test";

            if (isFirstBatchOfMessages)
            {
                isFirstBatchOfMessages = false;
                return;
            }

            var player = command.Substring(0, command.IndexOf(":") + 1);
            string audience = isMasterOnly ? playerName : command.Substring(0, command.IndexOf(":") + 1);
            Console.WriteLine(command);

            var task = new TaskFactory();

           if (command.Contains(audience) && command.Contains(botInstruction + " sa"))
            {
                isMasterOnly = !isMasterOnly;

                string response = isMasterOnly ? string.Format("I only obey master.") : string.Format("I am C-3P0, human cyborg relations");

                MessageToConsole(audience, response);

                return;
            }


           if (command.Contains(audience) && command.Contains(botInstruction))
           {
               int index = command.IndexOf(audience);
               string stuff = (index < 0)
                   ? command
                   : command.Remove(index, (audience + botInstruction).Length + 1).Replace("\r\n", "");

               Request r = new Request(stuff, myUser, myBot);
               Result res = myBot.Chat(r);
               var test = res.Output;

               MessageToConsole(audience.Replace(":", ""), res.Output, false);

               return;
           }

           //if (command.Contains(audience) && command.Contains(instruction1))
           //{
           //    int index = command.IndexOf(audience);
           //    string stuff = (index < 0)
           //        ? command
           //        : command.Remove(index, (audience + instruction1).Length + 1);

           //    task.StartNew(() => SendChatMessage(new StringBuilder(string.Format(" say ^1<^3{0}^1>{1}", audience.Replace(":", ""), translatorService.Translate(stuff)))))
           //        .ContinueWith(x =>
           //        {
           //            if (x.Status == TaskStatus.RanToCompletion)
           //            {
           //                SendChatMessage(new StringBuilder(" "));
           //            }
           //        });

           //    Console.WriteLine(stuff);
           //    return;
           //}


            if (command.Contains(playerName) && command.Contains(testCommand))
            {
                MessageToConsole("<3p0>", "test");

                return;
            }

            if (command.StartsWith(string.Format("{0} {1}", user, stopTranslatingInstruction)))
            {
                MessageToConsole("Threepio", string.Format("No longer translating ^1{0}^7...", TargetedPlayer));

                TargetedPlayer = "";

                return;
            }

            if (command.StartsWith(string.Format("{0}:", TargetedPlayer)))
            {
                var targetRemoval = command.Replace(string.Format("{0}:", TargetedPlayer), "");

                MessageToConsole(TargetedPlayer, targetRemoval, true);
                return;
            }

            if (command.StartsWith(string.Format("{0} {1}", user, instruction1)))
            {
                var partialName = command.Replace(string.Format("{0} {1}", user, translateInstruction), "").Trim();
                TargetedPlayer = serverManager.GetPlayer(partialName);

                if (string.IsNullOrEmpty(TargetedPlayer))
                {
                    MessageToConsole("Threepio", string.Format("Unable to find player with partial name: ^1{0}^7.", partialName));
                    return;
                }

                MessageToConsole("Threepio", string.Format("Now translating ^1{0}^7...", TargetedPlayer));
            }
        }

        private void MessageToConsole(string echoMessageReporter, string messageToDisplay, bool isTranslate = false)
        {
            //Block incoming async chat messages.
            IsSendingMessage = true;

            messageToDisplay = isTranslate ? translatorService.Translate(messageToDisplay) : messageToDisplay;
            var timeInterval = isTranslate ? 500 : 300;

            var task = new TaskFactory();
            task.StartNew(() => SendChatMessage(new StringBuilder(string.Format(" say ^1<^3{0}^1> {1}", echoMessageReporter, messageToDisplay))))
                .ContinueWith(x =>
                {
                    if (x.Status == TaskStatus.RanToCompletion)
                    {
                        //s          SendChatMessage(new StringBuilder(" "));
                    }

                    //we can now send more messages.
                    IsSendingMessage = false;
                });

        }              

        /// <summary>
        /// Send the chat message to the game console.
        /// </summary>
        /// <param name="textMessage">The message to be sent.</param>
        private void SendChatMessage(StringBuilder textMessage)
        {
            SendMessage(sendHandle, WM_SETTEXT, (IntPtr)textMessage.Length, textMessage.ToString());

           // if (!string.IsNullOrEmpty(textMessage.ToString()))
            System.Threading.Thread.Sleep(1000);
                PostMessage(sendHandle, WM_KEYDOWN, VK_RETURN, 0);
        }
    }
}
