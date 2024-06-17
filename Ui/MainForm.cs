using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;
using System.Reflection;
using System.Text.Json;
using System.Security.Policy;
using System.Runtime.InteropServices;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfUis;
using Splunk.Common;

using NM = Splunk.Common.NativeMethods;
using SU = Splunk.Common.ShellUtils;

using Win32BagOfTricks;
//using WBOT = Win32BagOfTricks;


namespace Splunk.Ui
{
    public partial class MainForm : Form
    {
        #region Definitions
        const int KEY_A = (int)Keys.A;
        #endregion

        #region Fields
        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("Splunk.Ui");

        /// <summary>App settings.</summary>
        readonly UserSettings _settings;

        /// <summary>Measure performance.</summary>
        readonly Stopwatch _sw = new();

        /// <summary>Hook message processing.</summary>
        readonly int _hookMsg;
        #endregion

        #region Lifecycle
        /// <summary>Constructor.</summary>
        public MainForm()
        {
            //_sw.Start();

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
            StartPosition = FormStartPosition.Manual;
            Location = _settings.FormGeometry.Location;
            Size = _settings.FormGeometry.Size;
            WindowState = FormWindowState.Normal;
            // Gets the icon associated with the currently executing assembly.
            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            // Info display.
            tvInfo.MatchColors.Add("ERR", Color.LightPink);
            tvInfo.BackColor = Color.Cornsilk;
            tvInfo.Prompt = ">";

            // Misc ui clickers.
            btnEdit.Click += (sender, e) => { SettingsEditor.Edit(_settings, "User Settings", 500); };
            btnDump.Click += (sender, e) => { SU.GetAppWindows("explorer").ForEach(w => tvInfo.AppendLine(w.ToString())); };

            // Manage commands in registry.
            btnInitReg.Click += (sender, e) => { _settings.Commands.ForEach(c => c.CreateRegistryEntry(Path.Join(Environment.CurrentDirectory, "Splunk.exe"))); };
            btnClearReg.Click += (sender, e) => { _settings.Commands.ForEach(c => c.RemoveRegistryEntry()); };

            // Shell hook handler.
            _hookMsg = NM.RegisterWindowMessage("SHELLHOOK"); // test for 0?
            NM.RegisterShellHookWindow(Handle);

            // Hot key handlers.
            NM.RegisterHotKey(Handle, MakeKeyId(KEY_A, NM.ALT | NM.CTRL | NM.SHIFT), NM.ALT | NM.CTRL | NM.SHIFT, KEY_A);

            // Debug stuff.
            btnGo.Click += (sender, e) => { DoSplunk(); };
            //btnGo.Click += (sender, e) => { CreateCommands(); };

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
                NM.UnregisterHotKey(Handle, MakeKeyId(KEY_A, NM.ALT | NM.CTRL | NM.SHIFT));

                components?.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Windows hooks
        /// <summary>
        /// Handle the hooked shell messages: shell window lifetime and hotkeys.
        /// </summary>
        /// <param name="message"></param>
        protected override void WndProc(ref Message message)
        {
            IntPtr handle = message.LParam;
            if (message.Msg == _hookMsg) // Window lifecycle.
            {
                NM.ShellEvents shellEvent = (NM.ShellEvents)message.WParam.ToInt32();

                switch (shellEvent)
                {
                    case NM.ShellEvents.HSHELL_WINDOWCREATED:
                        WindowInfo wi = SU.GetWindowInfo(handle);
                        _logger.Debug($"WindowCreatedEvent:{handle} {wi.Title}");
                        break;

                    case NM.ShellEvents.HSHELL_WINDOWACTIVATED:
                        //_logger.Debug($"WindowActivatedEvent:{handle}");
                        break;

                    case NM.ShellEvents.HSHELL_WINDOWDESTROYED:
                        //_logger.Debug($"WindowDestroyedEvent:{handle}");
                        break;
                }
            }
            else if (message.Msg == NM.WM_HOTKEY_MESSAGE_ID) // Decode key.
            {
                int key = (int)((long)message.LParam >> 16);
                int mod = (int)((long)message.LParam & 0xFFFF);

                if (mod == (NM.ALT | NM.CTRL | NM.SHIFT))
                {
                    switch (key)
                    {
                        case KEY_A:
                            _logger.Debug($"KEY_A:{handle}");
                            break;

                        default:
                            // Ignore
                            break;
                    }
                }
            }

            base.WndProc(ref message);
        }
        #endregion

        #region Internals
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

        /// <summary>
        /// Populate the settings with defined functions.
        /// </summary>
        void CreateCommands()
        {
            var rc = _settings.Commands; // alias
            rc.Clear();

            rc.Add(new("cmder", ExplorerContext.Dir, "Commander", "%SPLUNK %ID \"%D\"", "Open a new explorer next to the current."));
            rc.Add(new("tree", ExplorerContext.Dir, "Tree", "%SPLUNK %ID \"%D\"", "Copy a tree of selected directory to clipboard"));
            rc.Add(new("openst", ExplorerContext.Dir, "Open in Sublime", "\"%ProgramFiles%\\Sublime Text\\subl\" --launch-or-new-window \"%D\"", "Open selected directory in Sublime Text."));
            rc.Add(new("findev", ExplorerContext.Dir, "Find in Everything", "%ProgramFiles%\\Everything\\everything -parent \"%D\"", "Open selected directory in Everything."));
            rc.Add(new("tree", ExplorerContext.DirBg, "Tree", "%SPLUNK %ID \"%W\"", "Copy a tree here to clipboard."));
            rc.Add(new("openst", ExplorerContext.DirBg, "Open in Sublime", "\"%ProgramFiles%\\Sublime Text\\subl\" --launch-or-new-window \"%W\"", "Open here in Sublime Text."));
            rc.Add(new("findev", ExplorerContext.DirBg, "Find in Everything", "%ProgramFiles%\\Everything\\everything -parent \"%W\"", "Open here in Everything."));
            rc.Add(new("exec", ExplorerContext.File, "Execute", "%SPLUNK %ID \"%D\"", "Execute file if executable otherwise opened."));
            // test items:
            rc.Add(new("test_deskbg", ExplorerContext.DeskBg, "!! Test DeskBg", "%SPLUNK %ID \"%W\"", "Debug stuff."));
            rc.Add(new("test_folder", ExplorerContext.Folder, "!! Test Folder", "%SPLUNK %ID \"%D\"", "Debug stuff."));
        }

        /// <summary>
        /// Debug stuff.
        /// </summary>
        void DoSplunk()
        {
            //var fgHandle = NM.GetForegroundWindow();
            //WindowInfo fginfo = SU.GetWindowInfo(fgHandle);


            //List<string> args = ["cmder", @"C:\Users\cepth\AppData\Roaming\Sublime Text\Packages"];
            List<string> args = ["exec", @"C:\Dev\repos\Apps\Splunk\Test\go.cmd"];
            //List<string> args = ["exec", @"C:\Dev\repos\Apps\Splunk\Test\go.lua"];
            //List<string> args = ["test_deskbg", @"C:\Dev\repos\Apps\Splunk\Test\dummy.txt"];

            try
            {
                Splunk.Program.Run(args);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                // Get type, do something.
            }
        }
        #endregion
    }
}
