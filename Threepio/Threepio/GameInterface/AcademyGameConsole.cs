using System;
using System.Text;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Timers;
using System.Threading.Tasks;
using Threepio.Translator;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Threepio.GameInterface
{
    public class AcademyGameConsole
    {
        //private ServerManagement _severManagement;
        private TranslatorService translatorService;
        private List<string> Players { get; set; }
        private string TargetedPlayer { get; set; }

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

        public AcademyGameConsole(List<string> players)
        {
            Players = players;
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
            Timer timer = new Timer();
            timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            timer.Interval = 1000;
            timer.Enabled = true;
        }

        /// <summary>
        /// Retrieves chat messages every second.
        /// </summary>
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            GetChatMessages();
        }

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
        private void CheckMessageForCommand(string chatEntry)
        {
            //TODO: Refactor the string handling.
            var translateInstruction = ">";
            var stopTranslatingInstruction = ">>";
            
            if (isFirstBatchOfMessages)
            {
                isFirstBatchOfMessages = false;
                return;
            }
           

            var index = chatEntry.IndexOf(user);
            var userRemoval = (index < 0) ? chatEntry :  chatEntry.Remove(index, (user + translateInstruction).Length + 1).Replace("\r\n", "");

            var regex = new Regex("#(.*)#");
            var v = regex.Match(userRemoval);
            var partialName = userRemoval = v.Groups[1].ToString();

            

            if (chatEntry.StartsWith(string.Format("{0} {1}", user, stopTranslatingInstruction)))
            {
                TargetedPlayer = "";
                var task = new TaskFactory();
                task.StartNew(() => SendChatMessage(new StringBuilder(string.Format(" echo ^1<^3{0}^1>: Ended Translation...", "Threepio"))))
                   .ContinueWith(x =>
                   {
                       if (x.Status == TaskStatus.RanToCompletion)
                       {
                           SendChatMessage(new StringBuilder(" "));
                       }
                   });
                return;
            }

            if (chatEntry.StartsWith(TargetedPlayer + ":"))
            {
                var index2 = chatEntry.IndexOf(TargetedPlayer + ":");
                var targetRemoval = (index2 < 0) ? chatEntry : chatEntry.Remove(index2, (TargetedPlayer + ":" + translateInstruction).Length + 1).Replace("\r\n", "");
                //var messageToTranslate = (index < 0) ? chatEntry : chatEntry.Remove(index, (TargetedPlayer + ":" + translateInstruction).Length + 1)
                //.Replace("\r\n", "").Replace(string.Format("#{0}#", userRemoval), "");

                TranslateChatEntry(TargetedPlayer, targetRemoval);
                return;
            }            

            if (chatEntry.StartsWith(string.Format("{0} {1}", user, translateInstruction)))
            {
                var messageToTranslate = (index < 0) ? chatEntry : chatEntry.Remove(index, (TargetedPlayer + ":").Length + 1)
                .Replace("\r\n", "");

                TargetedPlayer = GetPlayer(partialName);
                var task = new TaskFactory();
                task.StartNew(() => SendChatMessage(new StringBuilder(string.Format(" echo ^1<^3{0}^1>: Now translating ^1{1}", "Threepio", TargetedPlayer))))
                   .ContinueWith(x =>
                   {
                       if (x.Status == TaskStatus.RanToCompletion)
                       {
                           SendChatMessage(new StringBuilder(" "));
                       }
                   });
            }
        }

        private void TranslateChatEntry(string targetedPlayer, string messageToTranslate)
        {
            var task = new TaskFactory();
               task.StartNew(() => SendChatMessage(new StringBuilder(string.Format(" echo ^1<^3{0}^1> {1}", targetedPlayer, translatorService.Translate(messageToTranslate)))))
                   .ContinueWith(x =>
                   {
                       if (x.Status == TaskStatus.RanToCompletion)
                       {
                           SendChatMessage(new StringBuilder(" "));
                       }
                   });
        }

        /// <summary>
        /// Returns a playerName based on the given partial name.
        /// </summary>
        /// <param name="partialPlayerName">The player's partial name.</param>
        /// <returns>The player name.</returns>
        public string GetPlayer(string partialName)
        {
            foreach (var player in Players)
            {
                if(player.ToLower().Contains(partialName.ToLower()))
                    return player;
            }
            return partialName;
        }

        /// <summary>
        /// Updates the list of players
        /// </summary>
        /// <param name="players">The list of players</param>
        public void UpdatePlayerList(List<string> players)
        {
            Players = players;
        }       

        /// <summary>
        /// Send the chat message to the game console.
        /// </summary>
        /// <param name="textMessage">The message to be sent.</param>
        private void SendChatMessage(StringBuilder textMessage)
        {
            SendMessage(sendHandle, WM_SETTEXT, (IntPtr)textMessage.Length, textMessage.ToString());

            if (!string.IsNullOrEmpty(textMessage.ToString()))
                PostMessage(sendHandle, WM_KEYDOWN, VK_RETURN, 0);
        }
    }
}
