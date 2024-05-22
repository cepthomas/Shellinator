using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using Ephemera.NBagOfTricks;
using Ipc = Ephemera.NBagOfTricks.SimpleIpc;
using Com = Splunk.Common.Common;


//TODO1 ? make into windows service like MassProcessing. Or at least run at startup.

//TODO1 ? generic script runner - Use for all? ps, cmd, lua, py, ... or Default/builtin

//TODO1 publishing and packaging: https://stackoverflow.com/questions/58994946/how-to-build-app-without-app-runtimeconfig-json





namespace Splunk
{
    public partial class MainForm : Form
    {
        /// <summary>The boilerplate.</summary>
        readonly Ipc.Server _server;

        /// <summary>The multiprocess log.</summary>
        readonly Ipc.MpLog _log;

        #region Lifecycle
        /// <summary>
        ///Constructor.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            CheckForIllegalCrossThreadCalls = true;

            tvInfo.AppendLine($"MainThread {System.Threading.Thread.CurrentThread.ManagedThreadId}");


            _log = new(Com.LogFileName, "SPLUNK");
            _log.Write("Hello from UI");

            // Info display.
            tvInfo.MatchColors.Add("ERROR ", Color.LightPink);
            tvInfo.BackColor = Color.Cornsilk;
            tvInfo.Prompt = ">";

            // Run server
            _server = new(Com.PIPE_NAME, Common.Common.LogFileName);
            _server.IpcReceive += Server_IpcReceive;
            _server.Start();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            // Current bin dir.
            var d = Environment.CurrentDirectory;

            //C:\Dev\repos\Apps\Splunk\Splunk\bin\Debug\net8.0-windows

            //            CreateRegistryEntries(d);

            base.OnLoad(e);
        }

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _server.Dispose();
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }
        #endregion


        int index = 100;
        private void btnGo_Click(object sender, EventArgs e)
        {
            Go();
        }

        private void Go()
        {
            tvInfo.AppendLine($"Go() {System.Threading.Thread.CurrentThread.ManagedThreadId}");

            Process process = new();
            ProcessStartInfo startInfo = new()
            {
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd",

                Arguments = $"/c echo {index++} Oscar 456 | clip",
                //Arguments = "echo 123 Oscar 456 | clip & exit",

                //Arguments = "echo >>>>>>Oscar"  //"/C copy /b Image1.jpg + Archive.rar Image2.jpg"
            };
            process.StartInfo = startInfo;
            process.Start();

            //process.WaitForExit(1000);
            // There is a fundamental difference when you call WaitForExit() without a time -out, it ensures that the redirected
            // stdout/ err have returned EOF.This makes sure that you've read all the output that was produced by the process.
            // We can't see what "onOutput" does, but high odds that it deadlocks your program because it does something nasty
            // like assuming that your main thread is idle when it is actually stuck in WaitForExit().
        }


        /// <summary>
        /// Client has something to say.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        void Server_IpcReceive(object? _, Ipc.IpcReceiveEventArgs e)
        {

            //tvInfo.AppendLine($"Server_IpcReceive {System.Threading.Thread.CurrentThread.ManagedThreadId}");

            //int copy = 999;
            //this.BeginInvoke(new Action(() => DoWork(e)));//, System.Windows.Threading.DispatcherPriority.Background, null);

            ////this.EventHandler temp = MyEvent;
            ////if (temp != null)
            ////{
            ////    temp();
            ////}


            //this.Invoke(Go);

            //this.BeginInvoke(Go);

            //return;

            try
            {
                string cmd;
                string path;
                string tag;
                string dir;

                if (e.Error)
                {
                    throw new($"ipc server error: {e.Message}");
                }

                // Process the command string. Should be like "command" "args".
                // Split and remove spaces.
                var parts = StringUtils.SplitByToken(e.Message, "\"");
                parts.RemoveAll(string.IsNullOrWhiteSpace);
                if (parts.Count != 3) { throw new($"invalid command format"); }
                cmd = parts[0];
                tag = parts[1];
                path = parts[2];

                // Check for valid path arg.
                if (!Path.Exists(path)) { throw new($"invalid path: {path}"); }
                FileAttributes attr = File.GetAttributes(path);
                dir = attr.HasFlag(FileAttributes.Directory) ? path : Path.GetDirectoryName(path)!;

                // Check for valid command and execute it.
                ProcessStartInfo pinfo = new()
                {
                    UseShellExecute = false, //true,
                    CreateNoWindow = true,
                    WorkingDirectory = dir,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    //RedirectStandardInput = true,
                    //RedirectStandardOutput = true,
                };

                switch (cmd)
                {
                    case "cmder":
                        // Open a new explorer window at the dir selected in the first one.
                        // Locate it on one side or other of the first, same size.
                        // TODO option for full screen?
                        pinfo.FileName = "cmd";
                        pinfo.Arguments = $"/c echo >>>>>cmder!! | clip";
                        break;

                    case "newtab":
                        // Open a new explorer tab in current window at the dir selected in the first one.
                        // Something like https://github.com/tariibaba/WinENFET/blob/main/src (autohotkey)./win-e.ahk
                        pinfo.FileName = "cmd";
                        pinfo.Arguments = $"/c echo >>>>>newtab!! | clip";
                        break;

                    case "tree":
                        pinfo.FileName = "cmd";
                        pinfo.Arguments = $"/C tree \"{dir}\" /a /f | clip";
                        break;

                    case "openst":
                        pinfo.FileName = "subl";
                        pinfo.Arguments = $"-n \"{dir}\"";
                        break;

                    case "find":
                        pinfo.FileName = "everything";
                        pinfo.Arguments = $"-parent \"{dir}\"";
                        pinfo.WorkingDirectory = @"C:\Program Files\Everything";
                        break;

                    default:
                        throw new($"command verb: {cmd}");
                }

                var proc = new Process() { StartInfo = pinfo };
                proc.Start();
                //                proc.WaitForExit();

                //                if (proc.ExitCode != 0) { throw new($"process exit code: {proc.ExitCode}"); }
            }
            catch (Exception ex) // handle errors
            {
                tvInfo.AppendLine("ERROR " + ex.Message);
                _log.Write("ERROR " + ex.Message);
            }
        }




        ///////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////

        record struct RegCommand(string RegPath, string Command, string Name, string Tag);

        readonly RegCommand[] _regCommands =
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

        /*
        path: "Directory"  "Directory\Background"  "DesktopBackground"
        mycmd: "cmder"  "tree"  "openst"  "find"  "newtab"
        name: "Two Pane"  "Tree" "Open in Sublime"  "Everything"  "Open In New Tab"

        mycmd => cmder  tree  openst  find  newtab
        ; => Right click in explorer-right-pane or windows-desktop with a directory selected.
        [HKEY_CURRENT_USER\Software\Classes\Directory\shell\{mycmd}]
        "MUIVerb"="{name}"
        [HKEY_CURRENT_USER\Software\Classes\Directory\shell\{mycmd}\command]
        @="CL_PATH\SplunkClient.exe" "{mycmd}" "%V"

        mycmd => tree   openst   find   
        ; => Right click in explorer-right-pane with nothing selected (background)
        [HKEY_CURRENT_USER\Software\Classes\Directory\Background\shell\{mycmd}]
        "MUIVerb"="{name}"
        [HKEY_CURRENT_USER\Software\Classes\Directory\Background\shell\{mycmd}\command]
        @="CL_PATH\SplunkClient.exe" "{mycmd}" "%V"

        mycmd => newtab
        ; => Right click in windows-desktop with nothing selected (background).
        [HKEY_CURRENT_USER\Software\Classes\DesktopBackground\shell\{mycmd}]
        "MUIVerb"="{name}"
        [HKEY_CURRENT_USER\Software\Classes\DesktopBackground\shell\{mycmd}\command]
        @="CL_PATH\SplunkClient.exe" "{mycmd}" "%V"

        */


        //Things like these?:
        //"Position"="Bottom"

        //remove _splunk keys


        void RemoveRegistryEntries()
        {

            //public void DeleteSubKeyTree(string subkey);



        }


        void CreateRegistryEntries(string clientPath)
        {
            var hkcu = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);

            var splunk_root = hkcu.OpenSubKey(@"Software\Classes", writable: true);


            // Ensure keys of interest are there.
            //var dir_shell = splunk_root.CreateSubKey(@"Directory\shell");
            //var dir_bg_shell = splunk_root.CreateSubKey(@"Directory\Background\shell");
            //var dtop_bg_shell = splunk_root.CreateSubKey(@"DesktopBackground\shell");

            //[HKEY_CURRENT_USER\Software\Classes\Directory\Background\shell\xxxx]

            //[HKEY_CURRENT_USER\Software\Classes\Directory\Background\shell\xxxx\command]

            foreach (var rc in _regCommands)
            //var rc = _regCommands[0];
            {

                //; template =>
                //[HKEY_CURRENT_USER\Software\Classes\{path}\shell\{mycmd}]
                //"MUIVerb"="{name}"
                //[HKEY_CURRENT_USER\Software\Classes\{path}\shell\{mycmd}\command]
                //@="CL_PATH\SplunkClient.exe" "{mycmd}" "%V"


                var subkey = $"{rc.RegPath}\\shell\\{rc.Command}";

                using (var k = splunk_root.CreateSubKey(subkey))
                {
                    Debug.WriteLine($"MUIVerb={rc.Name}");
                    k.SetValue("MUIVerb", rc.Name);
                }

                subkey += "\\command";

                using (var k = splunk_root.CreateSubKey(subkey))
                {
                    //cmd.exe /s /k pushd "%V"
                    //"C:\Program Files (x86)\Common Files\Microsoft Shared\MSEnv\VSLauncher.exe" "%1" source:Explorer

                    var scmd = $"\"{clientPath}\\SplunkClient.exe\" {rc.Command} {rc.Tag} \"%V\"";
                    Debug.WriteLine($"@={scmd}");
                    k.SetValue("", scmd);
                }

                //sss = splunk_root.CreateSubKey(subkey);

                //@="CL_PATH\SplunkClient.exe" "{mycmd}" "%V%"

                // sss.Close();
            }






            //dtop_bg_shell.Close();
            //dir_bg_shell.Close();
            //dir_shell.Close();
            splunk_root.Close();
            hkcu.Close();





            //public RegistryKey CreateSubKey(string subkey);

            //public object? GetValue(string? name);

            //public void SetValue(string? name, object value);

            //public void DeleteSubKey(string subkey);

            //public void DeleteSubKeyTree(string subkey);

            //public void DeleteValue(string name);







            //targetKey.
            //            public RegistryKey? OpenSubKey(string name, bool writable)


            /// <summary>Creates a new subkey, or opens an existing one.</summary>
            /// <param name="subkey">Name or path to subkey to create or open.</param>
            /// <returns>The subkey, or <b>null</b> if the operation failed.</returns>
            //public RegistryKey CreateSubKey(string subkey)





            /*
            @="CL_PATH\SplunkClient.exe" "command" "%V"
            registry Setting to be a REG_EXPAND_SZ

            xxx = cmder  tree  openst  find  newtab
            ; => Right click in explorer-right-pane or windows-desktop with a directory selected.
            [HKEY_CURRENT_USER\Software\Classes\Directory\shell\xxx]
            "MUIVerb"="Menu Item"
            [HKEY_CURRENT_USER\Software\Classes\Directory\shell\xxx\command]
            @="my_command.exe" "%V"


            xxx = tree   openst   find   
            ; => Right click in explorer-right-pane with nothing selected (background)
            [HKEY_CURRENT_USER\Software\Classes\Directory\Background\shell\xxx]
            "MUIVerb"="Menu Item"
            [HKEY_CURRENT_USER\Software\Classes\Directory\Background\shell\xxx\command]
            @="my_command.exe" "%V"


            xxx = newtab
            ; => Right click in windows-desktop with nothing selected (background).
            [HKEY_CURRENT_USER\Software\Classes\DesktopBackground\shell\xxx]
            "MUIVerb"="Menu Item"
            [HKEY_CURRENT_USER\Software\Classes\DesktopBackground\shell\xxx\command]
            @="my_command.exe" "%V"
            */




            //var knames = subkey.GetSubKeyNames();

            //var ddd = subkey.OpenSubKey(@"Directory\shell");

            //ddd = ddd.OpenSubKey(@"splunk_top_menu");

            //var vvv = ddd.GetValue("MUIVerb").ToString();

            //Computer\HKEY_CURRENT_USER\Software\Classes\Directory\shell\splunk_top_menu
            //Computer\HKEY_CURRENT_USER\Software\Classes\*\shell\splunk_top

            // ;Right click in explorer_right_pane or windows_desktop with a directory selected.
            // [HKEY_CURRENT_USER\Software\Classes\Directory\shell\menu_item]
            // @=""
            // "MUIVerb"="Menu Item"
            // ;The command to execute - arg is the directory.
            // [HKEY_CURRENT_USER\Software\Classes\Directory\shell\menu_item\command]
            // @="my_command.exe" "%V"
            //
            // ;Right click in explorer_right_pane with nothing selected (background)
            // [HKEY_CURRENT_USER\Software\Classes\Directory\Background\shell\menu_item]
            // @=""
            // "MUIVerb"="Menu Item"
            // ;The command to execute - arg is not used.
            // [HKEY_CURRENT_USER\Software\Classes\Directory\Background\shell\menu_item\command]
            // @="my_command.exe" "%V"
            //
            // ;Right click in windows_desktop with nothing selected (background).
            // [HKEY_CURRENT_USER\Software\Classes\DesktopBackground\shell\menu_item]
            // @=""
            // "MUIVerb"="Menu Item"
            // ;The command to execute - arg is not used.
            // [HKEY_CURRENT_USER\Software\Classes\DesktopBackground\shell\menu_item\command]
            // @="my_command.exe" "%V"
            //
            // ;Right click in explorer_left_pane (navigation) with a folder selected.
            // [HKEY_CURRENT_USER\Software\Classes\Folder\shell\menu_item]
            // @=""
            // "MUIVerb"="Menu Item"
            // ;The command to execute - arg is the folder.
            // [HKEY_CURRENT_USER\Software\Classes\Folder\shell\menu_item\command]
            // @="my_command.exe" "%V"
            //
            // ;Right click in explorer_right_pane or windows_desktop with a file selected (* for all exts).
            // [HKEY_CURRENT_USER\Software\Classes\*\shell\menu_item]
            // @=""
            // "MUIVerb"="Menu Item"
            // ;The command to execute - arg is the file name.
            // [HKEY_CURRENT_USER\Software\Classes\*\shell\menu_item\command]
            // @="my_command.exe" "%V"
        }
    }
}
