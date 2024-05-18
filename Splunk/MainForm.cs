using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Ipc = Ephemera.NBagOfTricks.SimpleIpc;
using Ephemera.NBagOfTricks;
//using Nbot = Ephemera.NBagOfTricks;
using Com = Splunk.Common.Common;

//using Splunk.Common;
//using Ephemera.NBagOfTricks;


//TODO1 make into windows service like MassProcessing.

//TODO1 clean up registry testcmds.


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
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        void Server_IpcReceive(object? _, Ipc.IpcReceiveEventArgs e)
        {
            string? smsg = null;

            if (e.Error)
            {
                smsg = $"ERROR Ipc: {e.Message}";
            }

            if (smsg is null)
            {
                // Process the command string. Should be like "A1 B2" "C3" => A1 B2,C3.

                // Split and remove spaces.
                var parts = StringUtils.SplitByToken(e.Message, "\"");
                parts.RemoveAll(string.IsNullOrWhiteSpace);

                if (parts.Count != 2)
                {
                    smsg = $"ERROR command parts: {e.Message}";
                }

                if (smsg is null)
                {
                    var path = parts[1];
                    Path.Exists(path);

                    switch (parts[0])
                    {
                        case "cmder":
                            //TODO1
                            break;

                        case "tree":
                            //tree /a /f arg[2].GetDir() | clip
                            break;

                        case "newtab":
                            //TODO1 open arg[2].GetDir() in new tab / window. Something like https://github.com/tariibaba/WinENFET/blob/main/src (autohotkey)./win-e.ahk
                            break;

                        case "stdir":
                            //subl - n arg[2].GetDir()
                            break;

                        default:
                            smsg = $"ERROR command verb: {parts[0]}";
                            break;
                    }
                }

                if (smsg is null)
                {
                    smsg = $"ERROR command: {e.Message}";
                    tvInfo.AppendLine(smsg);
                    _log.Write(smsg);
                }
            }

            if (smsg is not null)
            {
                tvInfo.AppendLine(smsg);
                _log.Write(smsg);
            }
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
