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

        /// <summary>Measure performance.</summary>
        readonly Stopwatch _sw = new();

        /// <summary>Hook message processing.</summary>
        readonly int _hookMsg;
        #endregion

        // //https://learn.microsoft.com/en-us/dotnet/api/system.threading.manualresetevent?view=net-8.0
        ManualResetEvent _newWindowEvent = new ManualResetEvent(false);

        /// <summary>New window created.</summary>
        IntPtr _newHandle = 0;


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
            // Gets the icon associated with the currently executing assembly.
            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            // Info display.
            tvInfo.MatchColors.Add("ERR", Color.LightPink);
            tvInfo.BackColor = Color.Cornsilk;
            tvInfo.Prompt = ">";

            btnEdit.Click += (sender, e) => { EditSettings(); };

            // Manage commands in registry.
            btnInitReg.Click += (sender, e) => { _settings.RegistryCommands.ForEach(c => RegistryUtils.CreateRegistryEntry(c, Path.Join(Environment.CurrentDirectory, "Splunk.exe"))); };
            btnClearReg.Click += (sender, e) => { _settings.RegistryCommands.ForEach(c => RegistryUtils.RemoveRegistryEntry(c)); };

            // Shell hook handler.
            // https://stackoverflow.com/questions/4544468/why-my-wndproc-doesnt-receive-shell-hook-messages-if-the-app-is-set-as-default
            //https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registershellhookwindow
            _hookMsg = NM.RegisterWindowMessage("SHELLHOOK"); // test for 0?
            _ = NM.RegisterShellHookWindow(Handle);

            // Hot key handlers.
            NM.RegisterHotKey(Handle, MakeKeyId(VIS_WINDOWS_KEY, NM.ALT + NM.CTRL + NM.SHIFT), NM.ALT + NM.CTRL + NM.SHIFT, VIS_WINDOWS_KEY);
            NM.RegisterHotKey(Handle, MakeKeyId(ALL_WINDOWS_KEY, NM.ALT + NM.CTRL + NM.SHIFT), NM.ALT + NM.CTRL + NM.SHIFT, ALL_WINDOWS_KEY);

            // Debug stuff.
            btnGo.Click += (sender, e) => { DoCmder(); };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShown(EventArgs e)
        {
            _sw.Stop();
            _logger.Debug($"Startup msec: {_sw.ElapsedMilliseconds}");
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

                _newWindowEvent?.Dispose();

                components?.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Settings
        /// <summary>
        /// Edit the common options in a property grid. TODO2 best way to handle these + writes to registry? settings_default.json?
        /// </summary>
        void EditSettings()
        {
            // // Make a copy for possible restoration.
            // Type t = _settings.GetType();
            // JsonSerializerOptions opts = new();
            // string original = JsonSerializer.Serialize(_settings, t, opts);

            // Doesn't detect changes in collections. Also needs some kind of cancel/restore. Also set width?
            var changes = SettingsEditor.Edit(_settings, "User Settings", 500);

            // Detect changes of interest.
            // foreach (var (name, cat) in changes)

            LogManager.MinLevelFile = _settings.FileLogLevel;
            LogManager.MinLevelNotif = _settings.NotifLogLevel;
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
                        _newHandle = handle;
                        // Signal event.
                        _newWindowEvent.Set();
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

                if (mod == NM.ALT + NM.CTRL + NM.SHIFT)
                {
                    if (key == VIS_WINDOWS_KEY)
                    {
                        _logger.Debug($"VIS_WINDOWS_KEY:{handle}");
                    }
                    else if (key == ALL_WINDOWS_KEY)
                    {
                        _logger.Debug($"ALL_WINDOWS_KEY:{handle}");
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
        #endregion

        #region Debug stuff
        void DoCmder()
        {
            try
            {
                // case "cmder": // Put in Splunk.exe when working.
                // Open a new explorer window at the dir selected in the first one.
                // Locate it on one side or other of the first, same size.
                // Option for full screen?
                //https://stackoverflow.com/questions/1190423/using-setwindowpos-in-c-sharp-to-move-windows-around

                // TODO1 handle errors consistently.

                var targetDirXXX = @"C:\Dev\SplunkStuff"; // TODO1 fake from cmd line path - the rt click dir

                // Get the current explorer path. Note: could also use the %W arg.
                var currentPath = Path.GetDirectoryName(targetDirXXX);

                _logger.Debug($"Before ShellExecute {_newHandle}");

                // Create the new explorer.
                var res = NM.ShellExecute(Handle, "explore", targetDirXXX, null, null, (int)NM.ShowCommands.SW_NORMAL); // SW_HIDE?
                if (res <= 32)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new($"ShellExecute() failed: {SU.XlatErrorCode(error)}");
                }

                // Wait for new window to be created. TODO1 fail
                _logger.Debug($"Before wait {_newHandle}");
                _ = _newWindowEvent.WaitOne();
                _logger.Debug($"After wait {_newHandle}");
                //Thread.Sleep(500);


                // Locate the two explorer windows.
                WindowInfo? currentExplorer = null;
                WindowInfo? newExplorer = null;

                var explorerWindows = SU.GetAppWindows("explorer");
                foreach (var win in explorerWindows)
                {
                    if (win.Title == currentPath) { currentExplorer = win; }
                    if (win.Title == targetDirXXX) { newExplorer = win; }
                }

                if (currentExplorer is null || newExplorer is null)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new($"Shouldn't happen {SU.XlatErrorCode(error)}");
                }

                _logger.Debug($"currentExplorer:{currentExplorer.Handle}");
                _logger.Debug($"newExplorer:{newExplorer.Handle}");

                // TODO1 Relocate the windows to taste.
                //Set the first window as foreground
                //send Windows key + left arrow
                //Set the second window as foreground
                //send Windows key + right arrow

                // This works sort of.
                // Width 1920  Height 1080
                int w = 900;
                int h = 900;
                int t = 50;
                int l = 50;

                bool b = NM.MoveWindow(currentExplorer.Handle, l, t, w, h, true);
                if (!b)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new($"MoveWindow() 1 failed: {SU.XlatErrorCode(error)}");
                }
                NM.BringWindowToTop(currentExplorer.Handle);

                b = NM.MoveWindow(newExplorer.Handle, l + w, t, w, h, true);
                if (!b)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new($"MoveWindow() 2 failed: {SU.XlatErrorCode(error)}");
                }
                NM.BringWindowToTop(newExplorer.Handle);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
            }

            _ = _newWindowEvent.Reset();
        }



        // void CreateCommands() use settings_default.json 
        // {
        //     var rc = _settings.RegistryCommands; // alias
        //     rc.Clear();

        //     rc.Add(new("test", "Directory", ">>>>> Test", "%SPLUNK %ID \"%D\"", "Debug stuff."));
        //     rc.Add(new("cmder", "Directory", "Commander", "%SPLUNK %ID \"%D\"", "Open a new explorer next to the current."));
        //     rc.Add(new("tree", "Directory", "Tree", "%SPLUNK %ID \"%D\"", "Copy a tree of selected directory to clipboard"));
        //     rc.Add(new("openst", "Directory", "Open in Sublime", "\"C:\\Program Files\\Sublime Text\\subl\" --launch-or-new-window \"%D\"", "Open selected directory in Sublime Text."));
        //     rc.Add(new("findev", "Directory", "Find in Everything", "C:\\Program Files\\Everything\\everything -parent \"%D\"", "Open selected directory in Everything."));
        //     rc.Add(new("tree", "Directory\\Background", "Tree", "%SPLUNK %ID \"%W\"", "Copy a tree here to clipboard."));
        //     rc.Add(new("openst", "Directory\\Background", "Open in Sublime", "\"C:\\Program Files\\Sublime Text\\subl\" --launch-or-new-window \"%W\"", "Open here in Sublime Text."));
        //     rc.Add(new("findev", "Directory\\Background", "Find in Everything", "C:\\Program Files\\Everything\\everything -parent \"%W\"", "Open here in Everything."));
        // }
        #endregion
    }
}
