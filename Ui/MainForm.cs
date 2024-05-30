using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfUis;
using Splunk.Common;
using NM = Splunk.Common.NativeMethods;


namespace Splunk.Ui
{
    public partial class MainForm : Form
    {
        #region Definitions
        const int VIS_WINDOWS_KEY = (int)Keys.W;
        const int ALL_WINDOWS_KEY = (int)Keys.A;
        #endregion

        #region Fields
        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("Splunk.Ui");

        /// <summary>App settings.</summary>
        readonly UserSettings _settings;

        readonly Stopwatch _sw = new();

        readonly int _shellHookMsg;
        #endregion

        #region Lifecycle
        /// <summary>Constructor.</summary>
        public MainForm()
        {
            _sw.Start();

            // Must do this first before initializing.
            string appDir = MiscUtils.GetAppDataDir("Splunk", "Ephemera");
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            InitializeComponent();

            // Init logging.
            LogManager.MinLevelFile = _settings.FileLogLevel;
            LogManager.MinLevelNotif = _settings.NotifLogLevel;
            LogManager.LogMessage += (_, e) => { tvInfo.AppendLine($"{e.Message}"); };
            LogManager.Run($"{appDir}\\log.txt", 100000);

            // Main form.
            Location = _settings.FormGeometry.Location;
            Size = _settings.FormGeometry.Size;
            WindowState = FormWindowState.Normal;
            //BackColor = _settings.BackColor;

            // Info display.
            tvInfo.MatchColors.Add("ERROR ", Color.LightPink);
            tvInfo.BackColor = Color.Cornsilk;
            tvInfo.Prompt = ">";

            btnEdit.Click += (sender, e) => { EditSettings(); };

            // Install commands in registry.
            btnInitReg.Click += (sender, e) => { _settings.RegistryCommands.ForEach(c => RegistryUtils.CreateRegistryEntry(c, Environment.CurrentDirectory)); };

            // Remove commands from registry.
            btnClearReg.Click += (sender, e) => { _settings.RegistryCommands.ForEach(c => RegistryUtils.RemoveRegistryEntry(c)); };

            // Shell hook handler.
            // https://stackoverflow.com/questions/4544468/why-my-wndproc-doesnt-receive-shell-hook-messages-if-the-app-is-set-as-default
            //https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registershellhookwindow
            _shellHookMsg = NM.RegisterWindowMessage("SHELLHOOK"); // test for 0?
            _ = NM.RegisterShellHookWindow(Handle);

            // Hot key handlers.
            NM.RegisterHotKey(Handle, MakeKeyId(VIS_WINDOWS_KEY, NM.ALT + NM.CTRL + NM.SHIFT), NM.ALT + NM.CTRL + NM.SHIFT, VIS_WINDOWS_KEY);
            NM.RegisterHotKey(Handle, MakeKeyId(ALL_WINDOWS_KEY, NM.ALT + NM.CTRL + NM.SHIFT), NM.ALT + NM.CTRL + NM.SHIFT, ALL_WINDOWS_KEY);

            // Debug stuff.
            btnGo.Click += BtnGo_Click;
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
        /// Clean up on shutdown.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            LogManager.Stop();

            // Save user settings.
            _settings.FormGeometry = new()
            {
                X = Location.X,
                Y = Location.Y,
                Width = Width,
                Height = Height
            };

            _settings.Save();

            base.OnFormClosing(e);
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
                NM.UnregisterHotKey(Handle, MakeKeyId(VIS_WINDOWS_KEY, NM.ALT + NM.CTRL + NM.SHIFT));
                NM.UnregisterHotKey(Handle, MakeKeyId(ALL_WINDOWS_KEY, NM.ALT + NM.CTRL + NM.SHIFT));

                components?.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion

        /// <summary>
        /// Edit the common options in a property grid.
        /// </summary>
        void EditSettings()
        {
            var changes = SettingsEditor.Edit(_settings, "User Settings", 500);

            // Detect changes of interest.
            bool restart = false;

            foreach (var (name, cat) in changes)
            {
                switch (name)
                {
                    case "TODO2":
                        restart = true;
                        break;
                }
            }

            if (restart)
            {
                MessageBox.Show("Restart required for device changes to take effect");
            }
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
        /// Helper.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="mod"></param>
        /// <returns></returns>
        int MakeKeyId(int key, int mod = 0)
        {
            return mod ^ key ^ Handle.ToInt32();
        }


        /////////////////// Temp debug stuff ///////////////////


        void BtnGo_Click(object? sender, EventArgs e)
        {
            // Current bin dir. C:\Dev\repos\Apps\Splunk\Splunk\bin\Debug\net8.0-windows
            // ==== CreateRegistryEntries(Environment.CurrentDirectory);

            InitCommands();

            var wins = ShellUtils.GetAppWindows("explorer");


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



        void InitCommands()
        {
            var rc = _settings.RegistryCommands; // alias
            rc.Clear();

            rc.Add(new("test", "Directory", ">>>>> Test", "%SPLUNK %ID \"%V\""));
            rc.Add(new("cmder", "Directory", "Two Pane", "%SPLUNK %ID \"%V\""));
            rc.Add(new("tree", "Directory", "Tree", "cmd /c tree /a /f \"%V\" | clip"));
            rc.Add(new("openst", "Directory", "Open in Sublime", "subl -n \"%V\""));
            rc.Add(new("find", "Directory", "Find in Everything", "C:\\Program Files\\Everything\\everything -parent \"%V\""));
            rc.Add(new("newtab", "Directory", "Open in New Tab", "%SPLUNK %ID \"%V\""));
            rc.Add(new("tree", "Directory\\Background", "Tree", "cmd /c tree /a /f \"%V\" | clip"));
            rc.Add(new("openst", "Directory\\Background", "Open in Sublime", "subl -n \"%V\""));
            rc.Add(new("find", "Directory\\Background", "Find in Everything", "C:\\Program Files\\Everything\\everything -parent \"%V\""));
            rc.Add(new("newtab", "DesktopBackground", "Open in New Tab", "%SPLUNK %ID \"%V\""));

            // | Id      | Description | RegPath |
            // | -----   | ----------- | ------- |
            // | cmder   | Open a second explorer in dir - aligned with first.   | Directory |
            // | tree    | Cmd line to clipboard for current or sel dir.         | Directory, Directory\Background |
            // | newtab  | Open dir in new tab in current explorer.              | Directory, DesktopBackground |
            // | openst  | Open dir in Sublime Text.                             | Directory, Directory\Background |
            // | find    | Open dir in Everything.                               | Directory, Directory\Background |

        }

    }
}
