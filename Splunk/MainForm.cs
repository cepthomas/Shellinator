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

        ///// <summary>
        /////
        ///// </summary>
        ///// <param name="e"></param>
        //protected override void OnLoad(EventArgs e)
        //{
        //    DoRegistry();

        //    base.OnLoad(e);
        //}

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
                _server.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion

        /// <summary>
        ///
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        void Server_IpcReceive(object? _, Ipc.IpcReceiveEventArgs e)
        {
            try
            {
                string cmd;
                string path;
                string dir;
                //string? cmd = null;
                //string? path = null;
                //string? dir = null;

                if (e.Error)
                {
                    throw new($"ipc server error: {e.Message}");
                }

                // Process the command string. Should be like "command" "args".
                // Split and remove spaces.
                var parts = StringUtils.SplitByToken(e.Message, "\"");
                parts.RemoveAll(string.IsNullOrWhiteSpace);
                if (parts.Count != 2) { throw new($"invalid command format"); }
                cmd = parts[0];
                path = parts[1];

                // Check for valid path arg.
                if (!Path.Exists(path)) { throw new($"invalid path: {path}"); }
                FileAttributes attr = File.GetAttributes(path);
                dir = attr.HasFlag(FileAttributes.Directory) ? path : Path.GetDirectoryName(path)!;

                // Check for valid command and execute it.
                ProcessStartInfo pinfo = new()
                {
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WorkingDirectory = dir
                };

                switch (cmd)
                {
                    case "cmder":
                        // Open a new explorer window at the dir selected in the first one.
                        // Locate it on one side or other of the first, same size.
                        // TODO option for full screen?

                        break;

                    case "newtab":
                        // Open a new explorer tab in current window at the dir selected in the first one.

                        // Something like https://github.com/tariibaba/WinENFET/blob/main/src (autohotkey)./win-e.ahk
                        break;


                    case "tree":
                        pinfo.FileName = "cmd";
                        pinfo.Arguments = $"/C tree {dir} /a /f | clip";
                        break;

                    case "stdir":
                        pinfo.FileName = "subl";
                        pinfo.Arguments = $"-n {dir}";
                        break;

                    case "find":
                        pinfo.FileName = "everything";
                        pinfo.Arguments = $"-parent {dir}";
                        pinfo.WorkingDirectory = @"C:\Program Files\Everything";
                        break;

                    default:
                        throw new($"command verb: {cmd}");
                }

                var proc = new Process() { StartInfo = pinfo };
                proc.Start();
                proc.WaitForExit();

                if (proc.ExitCode != 0) { throw new($"process exit code: {proc.ExitCode}"); }
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
        void DoRegistry()
        {
            //using Microsoft.Win32;

            var hkcu = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
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
