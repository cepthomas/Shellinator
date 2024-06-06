using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;
using System.Reflection;
using System.Text.Json;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfUis;
using Splunk.Common;
using NM = Splunk.Common.NativeMethods;
using SU = Splunk.Common.ShellUtils;
using System.Security.Policy;


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

            // Install commands in registry.
            btnInitReg.Click += (sender, e) => { _settings.RegistryCommands.ForEach(c => RegistryUtils.CreateRegistryEntry(c, Path.Join(Environment.CurrentDirectory, "Splunk.exe"))); };

            // Remove commands from registry.
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





        public List<(string name, string cat)> Edit(object settings, string title, int height, bool expand = false)
        {
            // Make a copy for possible restoration.
            Type t = settings.GetType();
            JsonSerializerOptions opts = new();
            string original = JsonSerializer.Serialize(settings, t, opts);

            PropertyGrid pg = new()
            {
                Dock = DockStyle.Fill,
                PropertySort = PropertySort.Categorized,
                SelectedObject = settings
            };

            //if (settings is SettingsCore)
            //{
            //    pg.AddButton("Clear recent", null, "Clear recent file list", (_, __) => (settings as SettingsCore)!.RecentFiles.Clear());
            //}

            using Form f = new()
            {
                Text = title,
                AutoScaleMode = AutoScaleMode.None,
                Location = Cursor.Position,
                StartPosition = FormStartPosition.Manual,
                FormBorderStyle = FormBorderStyle.SizableToolWindow,
                ShowIcon = false,
                ShowInTaskbar = false
            };
            f.ClientSize = new(450, height); // do after construction

            // Detect changes of interest.
            List<(string name, string cat)> changes = new();

            //pg.PropertyGridExChange += Pg_PropertyGridExChange;

            pg.PropertyValueChanged += Pg_PropertyValueChanged;



            //{
            //    changes.Add((args.ChangedItem!.PropertyDescriptor!.Name, args.ChangedItem.PropertyDescriptor.Category));
            //};

            if (expand)
            {
                pg.ExpandAllGridItems();
            }

            f.Controls.Add(pg);

            f.ShowDialog();

            return changes;
        }

        private void Pg_PropertyValueChanged(object? s, PropertyValueChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        //private void Pg_PropertyGridExChange(object? sender, PropertyGridEx.PropertyGridExEventArgs e)
        //{
        //    throw new NotImplementedException();
        //}







        /// <summary>
        /// Edit the common options in a property grid.
        /// </summary>
        void EditSettings()
        {
            //var changes = SettingsEditor.Edit(_settings, "User Settings", 500);
            var changes = Edit(_settings, "User Settings", 500);

            // Detect changes of interest. TODO2
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
        /// Handle the hooked shell messages: shell window lifetime and hotkeys. Not used currently.
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
                        _logger.Debug($"WindowCreatedEvent:{handle}");
                        break;

                    case NM.ShellEvents.HSHELL_WINDOWACTIVATED:
                        _logger.Debug($"WindowActivatedEvent:{handle}");
                        break;

                    case NM.ShellEvents.HSHELL_WINDOWDESTROYED:
                        _logger.Debug($"WindowDestroyedEvent:{handle}");
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


        #region Debug stuff

        void BtnGo_Click(object? sender, EventArgs e)
        {
            InitCommands();

            // DoTree();

            DoCmder();


            void InitCommands()
            {
                var rc = _settings.RegistryCommands; // alias
                rc.Clear();

                rc.Add(new("test", "Directory", ">>>>> Test", "%SPLUNK %ID \"%D\"", "Debug stuff."));
                rc.Add(new("cmder", "Directory", "Commander", "%SPLUNK %ID \"%D\"", "Open a new explorer next to the current."));
                rc.Add(new("tree", "Directory", "Tree", "%SPLUNK %ID \"%D\"", "Copy a tree of selected directory to clipboard"));
                //rc.Add(new("tree", "Directory", "Tree", "cmd /c tree /a /f \"%D\" | clip", "???"));
                rc.Add(new("openst", "Directory", "Open in Sublime", "\"C:\\Program Files\\Sublime Text\\subl\" --launch-or-new-window \"%D\"", "Open selected directory in Sublime Text."));
                rc.Add(new("findev", "Directory", "Find in Everything", "C:\\Program Files\\Everything\\everything -parent \"%D\"", "Open selected directory in Everything."));
                rc.Add(new("tree", "Directory\\Background", "Tree", "%SPLUNK %ID \"%W\"", "Copy a tree here to clipboard."));
                // rc.Add(new("tree", "Directory\\Background", "Tree", "cmd /c tree /a /f \"%D\" | clip", "???"));
                rc.Add(new("openst", "Directory\\Background", "Open in Sublime", "\"C:\\Program Files\\Sublime Text\\subl\" --launch-or-new-window \"%W\"", "Open here in Sublime Text."));
                rc.Add(new("findev", "Directory\\Background", "Find in Everything", "C:\\Program Files\\Everything\\everything -parent \"%W\"", "Open here in Everything."));
            }



            void DoCmder()
            {
                // case "cmder": // Put in Splunk.exe when working.
                // TODO1 Open a new explorer window at the dir selected in the first one.
                // Locate it on one side or other of the first, same size.
                // Option for full screen?
                //https://stackoverflow.com/questions/1190423/using-setwindowpos-in-c-sharp-to-move-windows-around



                /*
                >>> With no explorers
                Title[Program Manager] Geometry[X: 0 Y: 0 W: 1920 H: 1080] IsVisible[True] Handle[65872] Pid[5748]

                >>> With two explorers, 1 tab, 2 tab
                Title[Program Manager] Geometry[X: 0 Y: 0 W: 1920 H: 1080] IsVisible[True] Handle[65872] Pid[5748]
                Title[C:\Users\cepth\OneDrive\OneDriveDocuments] Geometry[X: 501 Y: 0 W: 1258 H: 923] IsVisible[True] Handle[265196] Pid[5748]
                Title[C:\Dev] Geometry[X: 469 Y: 94 W: 1258 H: 923] IsVisible[True] Handle[589906] Pid[5748]
                or  this:
                Title[Home] Geometry[X: 469 Y: 94 W: 1258 H: 923] IsVisible[True] Handle[589906] Pid[5748]
                */

                // "CommandLine": "%SPLUNK %ID \u0022%V\u0022",
                // arg1:cmder  arg2:dir-where-clicked



                // Builtin keys:
                // Open folder in new window: Ctrl+N
                // Open a new tab (in Home): Ctrl+T
                // Open selected dir in new tab: MouseButtons.Middle

                var targetDir = @"C:\Dev\SplunkStuFf"; // <== fake from cmd line path

                // Get the current path. Could use the %W arg.
                var path = Path.GetDirectoryName(targetDir);

                // Locate the explorer window that generated the click.
                var explWins = SU.GetAppWindows("explorer");
                WindowInfo? clicked = null;

                if (explWins.Count == 0)
                {
                    throw new("No visible explorers. Shouldn't happen.");
                }
                else
                {
                    foreach (var win in explWins)
                    {
                        if (win.Title.ToLower() == path.ToLower())
                        {
                            clicked = win;
                        }
                        // Title is the selected tab contents aka dir shown in right pane.
                        // tvInfo.AppendLine($"EXPL:{win.Title}");
                    }
                }

                if (clicked is not null)
                {
                    //var dirToOpen = @"C:\Dev\SplunkStuff"; // ==>
                    // Open clickDir in a new explorer.  test=C:\Dev\SplunkStuff


                    /// <summary>Performs an operation on a specified file. See ShellExecuteEx() for args.</summary>
                    /// Example opens a URL in the default browser:
                    /// IntPtr result = ShellExecute(IntPtr.Zero, "open", "http://www.google.com", null, null, SW_NORMAL);
                    //public static extern IntPtr ShellExecute(IntPtr hwnd, string lpVerb, string lpFile,
                    //string lpParameters, string lpDirectory, int nShow);

                    // Show hidden. SWP_NOZORDER | SWP_NOSIZE | SWP_NOACTIVATE);
                    //public const short SWP_NOMOVE = 0X2;
                    //public const short SWP_NOSIZE = 0X01;
                    //public const short SWP_NOZORDER = 0X04;
                    //public const int SWP_SHOWWINDOW = 0x0040;

                    IntPtr handle = NM.ShellExecute(Handle, "explore", targetDir, null, null, (int)NM.SWP_NOZORDER);
                    // Move it.
                    var r = clicked.DisplayRectangle;
                    //bool b = NM.MoveWindow(handle, r.Left + r.Width, r.Top, r.Width, r.Height, true);

                    bool b = NM.SetWindowPos(handle, clicked.Handle, r.Left + r.Width, r.Top, r.Width, r.Height, (int)NM.ShowCommands.SW_NORMAL);

                    // show it.
                    //NM.ShowWindow(handle, (int)NM.ShowCommands.SW_NORMAL);

                }
                else
                {
                    throw new("Couldn't find. Shouldn't happen.");
                }

            }



            ////////////////////////////////////////////////////////////
            // Run using direct shell command.
            // case "tree": // direct => cmd /c /q tree /a /f "%V" | clip
            // still flashes, ? Try ShellExecuteEx, setting nShow=SW_HIDE. https://learn.microsoft.com/en-us/windows/win32/shell/launch
            //cmd /B tree /a /f "C:\Dev\SplunkStuff\test_dir" | clip
            //NM.SHELLEXECUTEINFO info = new();
            //info.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(info);
            //info.lpVerb = "open";
            ////info.lpFile = "cmd";
            ////info.lpParameters = "/B tree /a /f \"C:\\Dev\\SplunkStuff\\test_dir\" | clip";
            //info.lpFile = "cmd.exe";
            ////info.lpParameters = "tree /a /f \"C:\\Dev\\SplunkStuff\\test_dir\" | clip";
            ////info.lpParameters = "echo dooda > _dump.txt";
            //info.lpParameters = "type Ui.deps.json";
            //info.nShow = (int)NM.ShowCommands.SW_SHOW; //SW_HIDE SW_SHOW
            //info.fMask = (int)NM.ShellExecuteMaskFlags.SEE_MASK_NO_CONSOLE; // SEE_MASK_DEFAULT;
            //bool b = NM.ShellExecuteEx(ref info);
            //if (b == false || info.hInstApp < 32)
            //{
            //    Debug.WriteLine("!!!");
            //}
            //
            //If the function succeeds, it sets the hInstApp member of the SHELLEXECUTEINFO structure to a value greater than 32.
            //If the function fails, hInstApp is set to the SE_ERR_XXX error value that best indicates the cause of the failure.
            //Although hInstApp is declared as an HINSTANCE for compatibility with 16 - bit Windows applications, it is not a
            //true HINSTANCE. It can be cast only to an int and can be compared only to either the value 32 or the SE_ERR_XXX error codes.
            //The SE_ERR_XXX error values are provided for compatibility with ShellExecute.To retrieve more accurate error information,
            //use GetLastError. It may return one of the following values.
        }
        #endregion
    }
}
