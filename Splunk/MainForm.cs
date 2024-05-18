using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Ipc = Ephemera.NBagOfTricks.SimpleIpc;
using Com = Splunk.Common.Common;


//TODO1 make into windows service like MassProcessing.

//TODO1 clean up registry testcmds.


namespace Splunk
{
    public partial class MainForm : Form
    {
        readonly Ipc.MpLog _log;

        Ipc.Server server;



        public MainForm()
        {
            InitializeComponent();

            _log = new(Com.LogFileName, "SPLUNK");
            _log.Write("Hello from UI");

            // Text control.
            tvInfo.MatchColors.Add("ERR ", Color.Purple);
            // tvInfo.MatchColors.Add("55", Color.Green);
            tvInfo.BackColor = Color.Cornsilk;
            tvInfo.Prompt = ">";

            // FilTree.
            filTree.FilterExts = [".txt", ".ntr", ".md", ".xml", ".cs", ".py"];
            filTree.IgnoreDirs = [".vs", ".git", "bin", "obj", "lib"];
            filTree.RootDirs =
            [
                @"C:\Users\cepth\AppData\Roaming\Sublime Text\Packages\Notr",
                @"C:\Users\cepth\OneDrive\OneDriveDocuments\notes"
            ];
            //filTree.RecentFiles = new()
            //{
            //    @"C:\Dev\repos\repos_common\audio_file_info.txt",
            //    @"C:\Dev\repos\repos_common\build.txt"
            //};
            filTree.SplitterPosition = 40;
            filTree.SingleClickSelect = false;
            filTree.InitTree();
            //filTree.FileSelected += (object? sender, string fn) => { Tell($"Selected file: {fn}"); _settings.UpdateMru(fn); };
            //filTree.SingleClickSelect = _settings.SingleClickSelect;
            //filTree.SplitterPosition = _settings.SplitterPosition;

            // Run server
            server = new(Com.PIPE_NAME, Common.Common.LogFileName);
            server.IpcReceive += Server_IpcReceive;

            server.Start();
        }

        protected override void OnLoad(EventArgs e)
        {
            DoRegistry();

            base.OnLoad(e);
        }


        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
                server.Dispose();
            }
            base.Dispose(disposing);
        }

        void Server_IpcReceive(object? sender, Ipc.IpcReceiveEventArgs e)
        {
            if (!e.Error)
            {
                // Process the command string.

                //All client commands are of the form:
                //"CL_PATH\SplunkClient.exe" "command" "%V"


                // Execute a command.
                // path = "C:\Dev\repos\Apps\Splunk\Client\bin\Debug\net8.0\SplunkClient.exe"

                //Commands:
                //"path\SplunkClient.exe" "cmder" "%V"
                //    TODO1
                //"path\SplunkClient.exe" "tree" "%V"
                //    tree /a /f arg[2].GetDir() | clip
                //"path\SplunkClient.exe" "newtab" "%V"
                //    TODO1 open arg[2].GetDir() in new tab / window. Something like https://github.com/tariibaba/WinENFET/blob/main/src (autohotkey)./win-e.ahk
                //"path\SplunkClient.exe" "stdir" "%V"
                //    subl -n arg[2].GetDir()

            }
            else
            {
                var smsg = $"ERROR {e.Message}";
                tvInfo.AppendLine(smsg);
                _log.Write(smsg);
            }


            var stat = e.Error ? "ERR" : "RCV";

            tvInfo.AppendLine($"{stat} {e.Message}");
            _log.Write($"{stat} {e.Message}");

        }

        void DoRegistry()
        {
            var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            var subkey = hkcu.OpenSubKey(@"Software\Classes");

            var knames = subkey.GetSubKeyNames();

            var ddd = subkey.OpenSubKey(@"Directory\shell");

            ddd = ddd.OpenSubKey(@"splunk_top_menu");

            var vvv = ddd.GetValue("MUIVerb").ToString();

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
            // @="my_command.exe"
            // 
            // ;Right click in windows_desktop with nothing selected (background).
            // [HKEY_CURRENT_USER\Software\Classes\DesktopBackground\shell\menu_item]
            // @=""
            // "MUIVerb"="Menu Item"
            // ;The command to execute - arg is not used.
            // [HKEY_CURRENT_USER\Software\Classes\DesktopBackground\shell\menu_item\command]
            // @="my_command.exe"
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
