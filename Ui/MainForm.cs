using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;

using Com = Splunk.Common.Common;
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

        // #region Events
        // public event Action<IntPtr> WindowCreatedEvent;

        // public event Action<IntPtr> WindowActivatedEvent;

        // public event Action<IntPtr> WindowDestroyedEvent;

        // public event Action KeypressArrangeVisibleEvent;

        // public event Action KeypressArrangeAllEvent;

        // //whf.KeypressArrangeVisibleEvent += Whf_KeypressArrangeVisibleEvent;
        // //whf.KeypressArrangeAllEvent += Whf_KeypressArrangeAllEvent;
        // #endregion


        //Things like these?:
        //"Position"="Bottom"

        //readonly RU.RegCommand[] _regCommands =
        //[
        //    new("Directory", "cmder", "Two Pane", "dir"),
        //    new("Directory", "tree", "Tree", "dir"),
        //    new("Directory", "openst", "Open in Sublime", "dir"),
        //    new("Directory", "find", "Open in Everything", "dir"),
        //    new("Directory", "newtab", "Open in New Tab", "dir"),
        //    new("Directory\\Background", "tree", "Tree", "dirbg"),
        //    new("Directory\\Background", "openst", "Open in Sublime", "dirbg"),
        //    new("Directory\\Background", "find", "Open in Everything", "dirbg"),
        //    new("DesktopBackground", "newtab", "Open in New Tab", "deskbg"),
        //];


        //%l – Long file name form of the first parameter. Note that Win32/64 applications will be passed the long file name, whereas Win16 applications get the short file name.Specifying %l is preferred as it avoids the need to probe for the application type.

        //%d – Desktop absolute parsing name of the first parameter (for items that don't have file system paths).

        //%v – For verbs that are none implies all.If there is no parameter passed this is the working directory.
        //%V should be used if you want directory name, ie.when you want to add your command on context menu when
        //  you click on background, not on a single file or a directory name. %L won't work in that case.

        //%w – The working directory.
        //A warning about %W: It is not always available and will throw a cryptic error message if used in your command value.For example, calling your context menu item on a drive's or a library folder's context menu will not initialize this variable.Avoid its use outside of a file handler's context menu entry.


        //%* – Replace with all parameters.
        //%~ – Replace with all parameters starting with and following the second parameter.
        //%0 or %1 – The first file parameter.For example "C:\Users\Eric\Desktop\New Text Document.txt". Generally this should be in quotes and the applications command line parsing should accept quotes to disambiguate files with spaces in the name and different command line parameters (this is a security best practice and I believe mentioned in MSDN).
        //%<n> (where<n> is 2-9) – Replace with the nth parameter.
        //%s – Show command.
        //%h – Hotkey value.
        //%i – IDList stored in a shared memory handle is passed here.


        //"C:\Dev\repos\Apps\Splunk\SplunkXXX\bin\Debug\net8.0\SplunkXXX.exe" cmder xyz "%V"
        //public record struct RegCommand(string RegPath, string Name, string Command);

        readonly RU.RegCommand[] _regCommands =
        [
            new("Directory", "Two Pane", "%SPLUNK cmder dir \"%V\""),
            new("Directory", "Tree", "cmd /c tree /a /f \"%V\" | clip"),
            new("Directory", "Open in Sublime", "subl -n \"%V\""),
            new("Directory", "Open in Everything", "C:\\Program Files\\Everything\\everything -parent \"%V\""),
            new("Directory", "Open in New Tab", "%SPLUNK newtab dir \"%V\""),
            new("Directory\\Background", "Tree", "cmd /c tree /a /f \"%V\" | clip"),
            new("Directory\\Background", "Open in Sublime", "subl -n \"%V\""),
            new("Directory\\Background", "Open in Everything", "C:\\Program Files\\Everything\\everything -parent \"%V\""),
            new("DesktopBackground", "Open in New Tab", "%SPLUNK newtab deskbg \"%V\""),
        ];

        //Command = "%SPLUNK_PATH cmder dir \"%V\""

        //case "tree": // direct => cmd /c tree /a /f \"%V\" | clip
        //case "openst": // direct "subl -n \"%V\"";
        //case "find": // direct "C:\Program Files\Everything\everything -parent \"%V\"";




        //var strsubkey = $"{rc.RegPath}\\shell\\{rc.Command}";
        //using (var k = splunk_root!.CreateSubKey(strsubkey))
        //{
        //    Debug.WriteLine($"MUIVerb={rc.Name}");
        //    k.SetValue("MUIVerb", rc.Name);
        //}

        //strsubkey += "\\command";
        //using (var k = splunk_root!.CreateSubKey(strsubkey))
        //{
        //    //cmd.exe /s /k pushd "%V"
        //    //"C:\Program Files (x86)\Common Files\Microsoft Shared\MSEnv\VSLauncher.exe" "%1" source:Explorer

        //    var scmd = $"\"{exePath}\" {rc.Command} {rc.Tag} \"%V\"";
        //    Debug.WriteLine($"@={scmd}");
        //    k.SetValue("", scmd);
        //}







        #region Lifecycle
        /// <summary>Constructor.</summary>
        public MainForm()
        {
            _sw.Start();

            InitializeComponent();

            // Init logging.
            string appDir = MiscUtils.GetAppDataDir("Splunk", "Ephemera");
            LogManager.MinLevelFile = LogLevel.Debug;
            LogManager.MinLevelNotif = LogLevel.Debug;
            LogManager.LogMessage += (_, e) => { tvInfo.AppendLine($"{e.Message}"); };
            LogManager.Run(Path.Join(appDir, "log.txt"), 100000);


            // _log = new(Com.LogFileName, "SPLUNK");
            // _log.Write("Hello from UI");

            // Info display.
            tvInfo.MatchColors.Add("ERROR ", Color.LightPink);
            tvInfo.BackColor = Color.Cornsilk;
            tvInfo.Prompt = ">";
            _logger.Debug($"MainForm() {Environment.CurrentManagedThreadId}");

            // Cotrols.
            btnGo.Click += BtnGo_Click;
            // Install commands in registry.
            btnInitReg.Click += (sender, e) =>
            {
                // Current bin dir. C:\Dev\repos\Apps\Splunk\Ui\bin\Debug\net8.0-windows
                string sdir = Environment.CurrentDirectory;
                //RU.CreateRegistryEntries(_regCommands, Environment.CurrentDirectory);
            };
            // Remove commands from registry.
            btnClearReg.Click += (sender, e) =>
            {
                RU.RemoveRegistryEntries(_regCommands);
            };


            ///// <summary>
            ///// Install commands in registry.
            ///// </summary>
            //void RegCommands()
            //{
            //}

            // windows handler - WindowsHookForm
            // https://stackoverflow.com/questions/4544468/why-my-wndproc-doesnt-receive-shell-hook-messages-if-the-app-is-set-as-default
            _shellHookMsg = NM.RegisterWindowMessage("SHELLHOOK"); // test for 0?
            //https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registershellhookwindow
            NM.RegisterShellHookWindow(Handle);

            // keys handler - WindowsHookForm
            RegisterKeyHandler(this, VIS_WINDOWS_KEY, NM.ALT + NM.CTRL + NM.SHIFT);
            RegisterKeyHandler(this, ALL_WINDOWS_KEY, NM.ALT + NM.CTRL + NM.SHIFT);
            static bool RegisterKeyHandler(Form form, int key, int mod = 0)
            {
                return NM.RegisterHotKey(form.Handle, mod ^ key ^ form.Handle.ToInt32(), mod, key);
            }


            // // Run server.
            // _server = new(Com.PIPE_NAME, Com.LogFileName);
            // _server.IpcReceive += Server_IpcReceive;
            // _server.Start();
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
                UnregisterKeyHandler(this, VIS_WINDOWS_KEY, NM.ALT + NM.CTRL + NM.SHIFT);
                UnregisterKeyHandler(this, ALL_WINDOWS_KEY, NM.ALT + NM.CTRL + NM.SHIFT);
                static bool UnregisterKeyHandler(Form form, int key, int mod = 0)
                {
                    return NM.UnregisterHotKey(form.Handle, mod ^ key ^ form.Handle.ToInt32());
                }
                
                // _server.Dispose();

                components?.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion

        // // --- temp debug stuff ---
        // private void Go()
        // {
        //     Log($"Go() {Environment.CurrentManagedThreadId}");
        //     int _which = 1;

        //     if (_which == 0)
        //     {
        //         ProcessStartInfo startInfo = new()
        //         {
        //             UseShellExecute = false,
        //             WindowStyle = ProcessWindowStyle.Hidden,
        //             FileName = "cmd",
        //             Arguments = $"/c echo Oscar {DateTime.Now.Millisecond} XYZ | clip",
        //         };

        //         Process process = new() { StartInfo = startInfo };
        //         process.Start();

        //         //process.WaitForExit(1000);
        //         // There is a fundamental difference when you call WaitForExit() without a time -out, it ensures that the redirected
        //         // stdout/ err have returned EOF.This makes sure that you've read all the output that was produced by the process.
        //         // We can't see what "onOutput" does, but high odds that it deadlocks your program because it does something nasty
        //         // like assuming that your main thread is idle when it is actually stuck in WaitForExit().
        //     }
        //     else
        //     {
        //         // Fake message.
        //         _message = "\"cmder\" \"dir\" \"C:\\Dev\\repos\\Apps\\Splunk\\Splunk\\obj\"";
        //         ProcessMessage();
        //     }

        // }
        // // --- temp debug stuff ---

        /// <summary>
        /// Debug.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnGo_Click(object? sender, EventArgs e)
        {
            // Current bin dir. C:\Dev\repos\Apps\Splunk\Splunk\bin\Debug\net8.0-windows
            // ==== CreateRegistryEntries(Environment.CurrentDirectory);
            
            SU.GetExplorerWindows();
        }

        /// <summary>
        /// Handle the hooked shell messages: shell window lifetime and hotkeys. TODO2
        /// </summary>
        /// <param name="message"></param>
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == _shellHookMsg)
            {
                // handle windows
                NM.ShellEvents shellEvent = (NM.ShellEvents)message.WParam.ToInt32();
                IntPtr windowHandle = message.LParam;

                switch (shellEvent)
                {
                    case NM.ShellEvents.HSHELL_WINDOWCREATED:
                        //do something WindowCreatedEvent?.Invoke(windowHandle);
                        //whf.WindowCreatedEvent += (data) => { ArrangeOneWindow(data); };
                        break;

                    case NM.ShellEvents.HSHELL_WINDOWACTIVATED:
                        //do something WindowActivatedEvent?.Invoke(windowHandle);
                        break;

                    case NM.ShellEvents.HSHELL_WINDOWDESTROYED:
                        //do something WindowDestroyedEvent?.Invoke(windowHandle);
                        break;
                }
            }
            else if (message.Msg == NM.WM_HOTKEY_MESSAGE_ID)
            {
                // handle keys
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

        // /// <summary>
        // /// Client has something to say. TODO2
        // /// </summary>
        // /// <param name="_"></param>
        // /// <param name="e"></param>
        // void Server_IpcReceive(object? _, Ipc.IpcReceiveEventArgs e)
        // {
        //     Log($"Server_IpcReceive() {Environment.CurrentManagedThreadId}");

        //     if (e.Error)
        //     {
        //         //_message = "";
        //         Log("ERROR " + e.Message);
        //         _log.Write("ERROR " + e.Message);
        //     }
        //     else
        //     {
        //         //_message = e.Message;
        //         Log("RCV " + e.Message);
        //         //BeginInvoke(ProcessMessage);
        //     }
        //}
    }
}
