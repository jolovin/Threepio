using System;
using System.Text;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Timers;
using System.Threading.Tasks;
using Threepio.Translator;

namespace Threepio.GameInterface
{
    public class AcademyGameConsole
    {
        //private ServerManagement _severManagement;
        private TranslatorService translatorService;

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

        public AcademyGameConsole()
        {
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
            var translateInstruction = "_3";
            
            if (isFirstBatchOfMessages)
            {
                isFirstBatchOfMessages = false;
                return;
            }

            var task = new TaskFactory();

            if (chatEntry.Contains(string.Format("{0} {1}", user, translateInstruction)))
            {                
                var index = chatEntry.IndexOf(user);
                var messageToTranslate = (index < 0) ? chatEntry : chatEntry.Remove(index, (user + translateInstruction).Length + 1).Replace("\r\n", "");

                task.StartNew(() => SendChatMessage(new StringBuilder(string.Format(" echo ^1<^3{0}^1>{1}", user.Replace(":", ""), translatorService.Translate(messageToTranslate)))))
                    .ContinueWith(x =>
                    {
                        if (x.Status == TaskStatus.RanToCompletion)
                        {
                            SendChatMessage(new StringBuilder(" "));
                        }
                    });
            }
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
