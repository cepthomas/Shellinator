using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using SC = Splunk.Common.Common;
using NM = Splunk.Common.NativeMethods;
using SU = Splunk.Common.ShellUtils;
using RU = Splunk.Common.RegistryUtils;


namespace Splunk.Ui
{

    public partial class MainForm : Form
    {
        const int VIS_WINDOWS_KEY = (int)Keys.W;

        const int ALL_WINDOWS_KEY = (int)Keys.A;

        readonly Stopwatch _sw = new();

        readonly int _shellHookMsg;

        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("Splunk.Ui");


        // Temp func.
        //void InitCommands()
        //{
        //    var rc = SC.Settings.RegistryCommands;
        //    rc.Clear();

        //    rc.Add(new() { RegPath = "Directory", Text = "Two Pane", Command = "%SPLUNK cmder dir \"%V\"" });
        //    rc.Add(new() { RegPath = "Directory", Text = "Tree", Command = "cmd /c tree /a /f \"%V\" | clip" });
        //    rc.Add(new() { RegPath = "Directory", Text = "Open in Sublime", Command = "subl -n \"%V\"" });
        //    rc.Add(new() { RegPath = "Directory", Text = "Open in Everything", Command = "C:\\Program Files\\Everything\\everything -parent \"%V\"" });
        //    rc.Add(new() { RegPath = "Directory", Text = "Open in New Tab", Command = "%SPLUNK newtab dir \"%V\"" });
        //    rc.Add(new() { RegPath = "Directory\\Background", Text = "Tree", Command = "cmd /c tree /a /f \"%V\" | clip" });
        //    rc.Add(new() { RegPath = "Directory\\Background", Text = "Open in Sublime", Command = "subl -n \"%V\"" });
        //    rc.Add(new() { RegPath = "Directory\\Background", Text = "Open in Everything", Command = "C:\\Program Files\\Everything\\everything -parent \"%V\"" });
        //    rc.Add(new() { RegPath = "DesktopBackground", Text = "Open in New Tab", Command = "%SPLUNK newtab deskbg \"%V\"" });
        //}

        void InitCommands()
        {
            var rc = SC.Settings.RegistryCommands; // alias
            rc.Clear();

            rc.Add(new("cmder", "Directory", "Two Pane", "%SPLUNK %ID \"%V\""));
            rc.Add(new("tree", "Directory", "Tree", "cmd /c tree /a /f \"%V\" | clip"));
            rc.Add(new("subl", "Directory", "Open in Sublime", "subl -n \"%V\""));
            rc.Add(new("everything", "Directory", "Open in Everything", "C:\\Program Files\\Everything\\everything -parent \"%V\""));
            rc.Add(new("newtab", "Directory", "Open in New Tab", "%SPLUNK %ID \"%V\""));
            rc.Add(new("tree", "Directory\\Background", "Tree", "cmd /c tree /a /f \"%V\" | clip"));
            rc.Add(new("subl", "Directory\\Background", "Open in Sublime", "subl -n \"%V\""));
            rc.Add(new("everything", "Directory\\Background", "Open in Everything", "C:\\Program Files\\Everything\\everything -parent \"%V\""));
            rc.Add(new("newtab", "DesktopBackground", "Open in New Tab", "%SPLUNK %ID \"%V\""));
        }


        #region Lifecycle
        /// <summary>Constructor.</summary>
        public MainForm()
        {
            _sw.Start();

            InitializeComponent();

InitCommands();

            // Init logging.
            string appDir = MiscUtils.GetAppDataDir("Splunk", "Ephemera");
            LogManager.MinLevelFile = LogLevel.Debug;
            LogManager.MinLevelNotif = LogLevel.Debug;
            LogManager.LogMessage += (_, e) => { tvInfo.AppendLine($"{e.Message}"); };
            LogManager.Run($"{appDir}\\log.txt", 100000);

            // Info display.
            tvInfo.MatchColors.Add("ERROR ", Color.LightPink);
            tvInfo.BackColor = Color.Cornsilk;
            tvInfo.Prompt = ">";
            //_logger.Debug($"MainForm() {Environment.CurrentManagedThreadId}");

            // Debug stuff.
            btnGo.Click += BtnGo_Click;

            // Install commands in registry.
            btnInitReg.Click += (sender, e) =>
            {
                // Current bin dir. C:\Dev\repos\Apps\Splunk\Ui\bin\Debug\net8.0-windows
                string sdir = Environment.CurrentDirectory;
                SC.Settings.RegistryCommands.ForEach(c => RU.CreateRegistryEntry(c, sdir));
            };

            // Remove commands from registry.
            btnClearReg.Click += (sender, e) =>
            {
                SC.Settings.RegistryCommands.ForEach(c => RU.RemoveRegistryEntry(c));
            };

            // windows handler - WindowsHookForm
            // https://stackoverflow.com/questions/4544468/why-my-wndproc-doesnt-receive-shell-hook-messages-if-the-app-is-set-as-default
            _shellHookMsg = NM.RegisterWindowMessage("SHELLHOOK"); // test for 0?
            //https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registershellhookwindow
            _ = NM.RegisterShellHookWindow(Handle);

            // keys handler - WindowsHookForm
            NM.RegisterHotKey(Handle, MakeKeyId(this, VIS_WINDOWS_KEY, NM.ALT + NM.CTRL + NM.SHIFT), NM.ALT + NM.CTRL + NM.SHIFT, VIS_WINDOWS_KEY);
            NM.RegisterHotKey(Handle, MakeKeyId(this, ALL_WINDOWS_KEY, NM.ALT + NM.CTRL + NM.SHIFT), NM.ALT + NM.CTRL + NM.SHIFT, ALL_WINDOWS_KEY);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShown(EventArgs e)
        {
            _sw.Stop();
            _logger.Debug($"Startup msec: {_sw.ElapsedMilliseconds}"); // 147 msec
            base.OnShown(e);
        }

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                NM.DeregisterShellHookWindow(Handle);
                NM.UnregisterHotKey(Handle, MakeKeyId(this, VIS_WINDOWS_KEY, NM.ALT + NM.CTRL + NM.SHIFT));
                NM.UnregisterHotKey(Handle, MakeKeyId(this, ALL_WINDOWS_KEY, NM.ALT + NM.CTRL + NM.SHIFT));

                components?.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion


        /// <summary>
        /// Debug.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BtnGo_Click(object? sender, EventArgs e)
        {
            // Current bin dir. C:\Dev\repos\Apps\Splunk\Splunk\bin\Debug\net8.0-windows
            // ==== CreateRegistryEntries(Environment.CurrentDirectory);


            var wins = SU.GetAppWindows("explorer");


            // public static string ExecInNewProcess2()
            // {
            //     string ret = "Nada";
            //     Process cmd = new();
            //     cmd.StartInfo.FileName = "cmd.exe";
            //     cmd.StartInfo.RedirectStandardInput = true;
            //     cmd.StartInfo.RedirectStandardOutput = true;
            //     cmd.StartInfo.CreateNoWindow = true;
            //     cmd.StartInfo.UseShellExecute = false;
            //     cmd.Start();
            //     cmd.StandardInput.WriteLine("echo !!Oscar");
            //     cmd.StandardInput.Flush();
            //     cmd.StandardInput.Close();
            //     cmd.WaitForExit(); // wait for the process to complete before continuing and process.ExitCode
            //     ret = cmd.StandardOutput.ReadToEnd();
            //     return ret;
            // }





            //Log($"Go() {Environment.CurrentManagedThreadId}");
            //int _which = 1;

            //if (_which == 0)
            //{
            //    ProcessStartInfo startInfo = new()
            //    {
            //        UseShellExecute = false,
            //        WindowStyle = ProcessWindowStyle.Hidden,
            //        FileName = "cmd",
            //        Arguments = $"/c echo Oscar {DateTime.Now.Millisecond} XYZ | clip",
            //    };

            //    Process process = new() { StartInfo = startInfo };
            //    process.Start();

            //    //process.WaitForExit(1000);
            //    // There is a fundamental difference when you call WaitForExit() without a time -out, it ensures that the redirected
            //    // stdout/ err have returned EOF.This makes sure that you've read all the output that was produced by the process.
            //    // We can't see what "onOutput" does, but high odds that it deadlocks your program because it does something nasty
            //    // like assuming that your main thread is idle when it is actually stuck in WaitForExit().
            //}
            //else
            //{
            //    // Fake message.
            //    _message = "\"cmder\" \"dir\" \"C:\\Dev\\repos\\Apps\\Splunk\\Splunk\\obj\"";
            //    ProcessMessage();
            //}
        }

        /// <summary>
        /// Handle the hooked shell messages: shell window lifetime and hotkeys. TODO2 do something with them?
        /// </summary>
        /// <param name="message"></param>
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == _shellHookMsg) // Window lifecycle.
            {
                NM.ShellEvents shellEvent = (NM.ShellEvents)message.WParam.ToInt32();
                IntPtr handle = message.LParam;

                switch (shellEvent)
                {
                    case NM.ShellEvents.HSHELL_WINDOWCREATED:
                        //WindowCreatedEvent?.Invoke(this, handle);
                        break;

                    case NM.ShellEvents.HSHELL_WINDOWACTIVATED:
                        //WindowActivatedEvent?.Invoke(this, handle);
                        break;

                    case NM.ShellEvents.HSHELL_WINDOWDESTROYED:
                        //WindowDestroyedEvent?.Invoke(this, handle);
                        break;
                }
            }
            else if (message.Msg == NM.WM_HOTKEY_MESSAGE_ID) // Decode key.
            {
                int key = (int)((long)message.LParam >> 16);
                int mod = (int)((long)message.LParam & 0xFFFF);

                if (mod == NM.ALT + NM.CTRL + NM.SHIFT)
                {
                    if (key == VIS_WINDOWS_KEY)
                    {
                        //do something KeypressArrangeVisibleEvent?.Invoke();
                    }
                    else if (key == ALL_WINDOWS_KEY)
                    {
                        //do something KeypressArrangeAllEvent?.Invoke();
                    }
                }
            }

            base.WndProc(ref message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="form"></param>
        /// <param name="key"></param>
        /// <param name="mod"></param>
        /// <returns></returns>
        int MakeKeyId(Form form, int key, int mod = 0)
        {
            return mod ^ key ^ form.Handle.ToInt32();
        }
    }
}
