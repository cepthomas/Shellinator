using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;
using Ipc = Ephemera.NBagOfTricks.SimpleIpc;
using Com = Splunk.Common.Common;
using System.Diagnostics;


//TODO1 make into windows service like MassProcessing. Or at least run at startup.

//TODO1 clean up registry testcmds.



// TODO fix nav bar + history. (re)Implement?
// TODO tag files/dirs. Use builtin libraries and/or favorites?
// TDOO more info/hover?. filters, fullpath, size, thumbnail



namespace Splunk
{
    public partial class MainForm : Form
    {
        /// <summary>The boilerplate.</summary>
        readonly Ipc.Server server;

        /// <summary>The multi log.</summary>
        readonly Ipc.MpLog _log;

        #region Lifecycle
        /// <summary>
        ///
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            _log = new(Com.LogFileName, "SPLUNK");
            _log.Write("Hello from UI");

            // Text control.
            tvInfo.MatchColors.Add("ERROR ", Color.LightPink);
            tvInfo.BackColor = Color.Cornsilk;
            tvInfo.Prompt = ">";

            // FilTree.
            filTree.SplitterPosition = 40;
            filTree.SingleClickSelect = false;
            filTree.Visible = false;
            //filTree.InitTree();
            //filTree.FileSelected += (object? sender, string fn) => { Tell($"Selected file: {fn}"); _settings.UpdateMru(fn); };
            //filTree.SingleClickSelect = _settings.SingleClickSelect;
            //filTree.SplitterPosition = _settings.SplitterPosition;

            // Run server
            server = new(Com.PIPE_NAME, Common.Common.LogFileName);
            server.IpcReceive += Server_IpcReceive;

            server.Start();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
    //        DoRegistry();

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
        #endregion

        /// <summary>
        ///
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        void Server_IpcReceive(object? _, Ipc.IpcReceiveEventArgs e)
        {
            string? serr = null;
            string? cmd = null;
            string? path = null;
            string? dir = null;
            bool? isdir = null;

            if (e.Error)
            {
                serr = $"ipc server error: {e.Message}";
            }

            // Process the command string. Should be like "command" "args".
            if (serr is null)
            {
                // Split and remove spaces.
                var parts = StringUtils.SplitByToken(e.Message, "\"");
                parts.RemoveAll(string.IsNullOrWhiteSpace);
                if (parts.Count != 2)
                {
                    serr = $"invalid command format";
                }
                else
                {
                    cmd = parts[0];
                    path = parts[1];
                }
            }

            // Check for valid path arg.
            if (serr is null)
            {
                if (Path.Exists(path))
                {
                    FileAttributes attr = File.GetAttributes(path);
                    if (attr.HasFlag(FileAttributes.Directory))
                    {
                        isdir = true;
                        dir = path;
                    }
                    else
                    {
                        isdir = false;
                        dir = Path.GetDirectoryName(path);
                    }
                }
                else
                {
                    serr = $"invalid path: {path}";
                }
            }

            // Check for valid command and execute it.
            if (serr is null)
            {
                Process? p = null;
                string? exe = null;
                string? args = null;

                switch (cmd)
                {
                    case "cmder":
                        //TODO1  option for full screen?
                        break;

                    case "tree":

                        exe = "dir";
                        args = $" | clip";

                        //exe = "tree";
                        //args = $"{dir} /a /f | clip";

                        //args = $"{dir} /a /f > C:\\Dev\\clip.txt";
                        break;

                    case "newtab":
                        // (explorer middle button?)
                        //TODO1 open arg[2].GetDir() in new tab / window. Something like https://github.com/tariibaba/WinENFET/blob/main/src (autohotkey)./win-e.ahk
                        break;

                    case "stdir":
                        exe = "subl";
                        args = $"-n {dir}";
                        break;

                    case "find":
                        exe = "everything";
                        args = $"-parent {dir}";
                        break;

                    default:
                        serr = $"command verb: {cmd}";
                        break;
                }

                if (exe is not null && args is not null)
                {
                    try
                    {
                        var info = new ProcessStartInfo()
                        {

                            FileName = Path.GetFileName(exe),
                            Arguments = args,
                            UseShellExecute = true,
                             CreateNoWindow = true,
                              ErrorDialog = true,
                        };

                        var proc = new Process()
                        {
                            StartInfo = info
                        };

                        if (proc is null)
                        {
                            serr = $"process execute failed: {exe} {args}";
                        }
                        else
                        {
                            proc.Start();
                        }
                    }
                    catch (Exception ex)
                    {
                        serr = $"execute failed: {ex.Message}";
                    }
                }
            }

            // Handle errors.
            if (serr is not null)
            {
                tvInfo.AppendLine("ERROR " + serr);
                _log.Write("ERROR " + serr);
            }
        }


        ///////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////
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
