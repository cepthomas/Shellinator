using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;
using Ephemera.NBagOfTricks;
using Ipc = Ephemera.NBagOfTricks.SimpleIpc;
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

        // #region Events
        // public event Action<IntPtr> WindowCreatedEvent;

        // public event Action<IntPtr> WindowActivatedEvent;

        // public event Action<IntPtr> WindowDestroyedEvent;

        // public event Action KeypressArrangeVisibleEvent;

        // public event Action KeypressArrangeAllEvent;

        // //whf.KeypressArrangeVisibleEvent += Whf_KeypressArrangeVisibleEvent;
        // //whf.KeypressArrangeAllEvent += Whf_KeypressArrangeAllEvent;
        // #endregion

        /// <summary>The boilerplate.</summary>
        readonly Ipc.Server _server;

        /// <summary>The multiprocess log.</summary>
        readonly Ipc.MpLog _log;



        readonly RU.RegCommand[] _regCommands =
        [
            new("Directory", "cmder", "Two Pane", "dir"),
            new("Directory", "tree", "Tree", "dir"),
            new("Directory", "openst", "Open in Sublime", "dir"),
            new("Directory", "find", "Open in Everything", "dir"),
            new("Directory", "newtab", "Open in New Tab", "dir"),
            new("Directory\\Background", "tree", "Tree", "dirbg"),
            new("Directory\\Background", "openst", "Open in Sublime", "dirbg"),
            new("Directory\\Background", "find", "Open in Everything", "dirbg"),
            new("DesktopBackground", "newtab", "Open in New Tab", "deskbg"),
        ];


        #region Lifecycle
        /// <summary>Constructor.</summary>
        public MainForm()
        {
            _sw.Start();

            InitializeComponent();

            _log = new(Com.LogFileName, "SPLUNK");
            _log.Write("Hello from UI");

            // Info display.
            tvInfo.MatchColors.Add("ERROR ", Color.LightPink);
            tvInfo.BackColor = Color.Cornsilk;
            tvInfo.Prompt = ">";
            tvInfo.AppendLine($"MainForm() {Environment.CurrentManagedThreadId}");

            btnGo.Click += BtnGo_Click;

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


            // Run server.
            _server = new(Com.PIPE_NAME, Com.LogFileName);
            _server.IpcReceive += Server_IpcReceive;
            _server.Start();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            //Hide();

            // Current bin dir. C:\Dev\repos\Apps\Splunk\Splunk\bin\Debug\net8.0-windows
            // ==== CreateRegistryEntries(Environment.CurrentDirectory);

            base.OnLoad(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShown(EventArgs e)
        {
            _sw.Stop();

            tvInfo.AppendLine($"dur::::{_sw.ElapsedMilliseconds}"); // 147 msec

            base.OnShown(e);
        }

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            /////////////// from WindowsHookForm //////////////////////
            if (disposing)
            {
                NM.DeregisterShellHookWindow(Handle);
                UnregisterKeyHandler(this, VIS_WINDOWS_KEY, NM.ALT + NM.CTRL + NM.SHIFT);
                UnregisterKeyHandler(this, ALL_WINDOWS_KEY, NM.ALT + NM.CTRL + NM.SHIFT);
                static bool UnregisterKeyHandler(Form form, int key, int mod = 0)
                {
                    return NM.UnregisterHotKey(form.Handle, mod ^ key ^ form.Handle.ToInt32());
                }
                
                _server.Dispose();

                components?.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion

        // // --- temp debug stuff ---
        // private void Go()
        // {
        //     tvInfo.AppendLine($"Go() {Environment.CurrentManagedThreadId}");
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



        private void BtnGo_Click(object? sender, EventArgs e)
        {
            SU.GetExplorerWindows();
        }



        /// <summary>
        /// Handle the hooked shell messages: shell window lifetime and hotkeys.
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


        /// <summary>
        /// Client has something to say.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        void Server_IpcReceive(object? _, Ipc.IpcReceiveEventArgs e)
        {
            tvInfo.AppendLine($"Server_IpcReceive() {Environment.CurrentManagedThreadId}");

            if (e.Error)
            {
                //_message = "";
                tvInfo.AppendLine("ERROR " + e.Message);
                _log.Write("ERROR " + e.Message);
            }
            else
            {
                //_message = e.Message;
                tvInfo.AppendLine("RCV " + e.Message);
                //BeginInvoke(ProcessMessage);
            }
        }


    }
}
