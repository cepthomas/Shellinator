using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Ipc = Ephemera.NBagOfTricks.SimpleIpc;
using Com = Splunk.Common.Common;


//TODO1 make into windows service like MassProcessing.

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
            server = new(Com.PipeName, Common.Common.LogFileName);
            server.IpcReceive += Server_IpcReceive;

            server.Start();
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
            var stat = e.Error ? "ERR" : "RCV";

// 42:41.270 SPLCLI 19564  1      C:\Dev\repos\Apps\Splunk\Splunk  %V
// 22:27.297 SPLCLI 11640  1      C:\Dev\repos\Apps\Splunk\menu1.jpg  %V
// 08:40.298 SPLCLI  2976  1      C:\Users\cepth\Desktop\Clipboard_05-13-2024_01.png


            tvInfo.AppendLine($"{stat} {e.Message}");
            _log.Write($"{stat} {e.Message}");
        }

//TODO1 clean up registry testcmd s.
        

        /* =====================
        Commander
        Open a second XP in dir - aligned with first. opt for full screen?
        src: Dir
        "path\SplunkClient.exe" "src" "cmder" "%V"

        Tree
        Cmd line to clipboard for current or sel dir
        src: dir/file/dirbg
        "path\SplunkClient.exe" "src" "tree" "%V"

        Dir
        Cmd line to clipboard for current or sel dir
        src: dir/file/dirbg
        "path\SplunkClient.exe" "src" "dir" "%V"

        Open in tab
        Open dir in tab in current window
        src: dir/desktop  (In XP use middle button)
        "path\SplunkClient.exe" "src" "newtab" "%V"
        TODO1 Desktop (other?) items need open in new tab / window. Might need something like https://github.com/tariibaba/WinENFET/blob/main/src (autohotkey)./win-e.ahk

        Open dir in sublime
        Open dir in a new ST
        src: dir/dirbg
        "path\SplunkClient.exe" "src" "stdir" "%V"
        ST cl:
        Usage: subl [arguments] [files]         Edit the given files
           or: subl [arguments] [directories]   Open the given directories
           or: subl [arguments] -- [files]      Edit files that may start with '-'
           or: subl [arguments] -               Edit stdin
           or: subl [arguments] - >out          Edit stdin and write the edit to stdout
        Arguments:
          --project <project>:    Load the given project
          --command <command>:    Run the given command
          -n or --new-window:     Open a new window
          --launch-or-new-window: Only open a new window if the application is open
          -a or --add:            Add folders to the current window
          -w or --wait:           Wait for the files to be closed before returning
          -b or --background:     Don't activate the application
          -s or --stay:           Keep the application activated after closing the file
          --safe-mode:            Launch using a sandboxed (clean) environment
          -h or --help:           Show help (this message) and exit
          -v or --version:        Show version and exit
        Filenames may be given a :line or :line:column suffix to open at a specific location.
        */

        /*
        HKEY_CLASSES_ROOT\Directory\shell\testcmd -> Rt Click on a selected dir
        @====> Test - Directory
        [HKEY_CLASSES_ROOT\Directory\shell\testcmd\command]
        orig: @="cmd.exe /k \"echo %A`%B`%C`%D`%E`%F`%G`%H`%I`%J`%K`%L`%M`%N`%O`%P`%Q`%R`%S`%T`%U`%V`%W`%X`%Y`%Z\""
        @="C:\Dev\repos\Apps\Splunk\Client\bin\Debug\net8.0\SplunkClient.exe" "DirShell" "%S" "%H" "%L" "%D" "%V" "%W"
        args:
        42:41.268 SPLCLI 19564  1      C:\Dev\repos\Apps\Splunk\Client\bin\Debug\net8.0\SplunkClient.dll
        42:41.268 SPLCLI 19564  1      DirShell
        42:41.269 SPLCLI 19564  1      1
        42:41.269 SPLCLI 19564  1      0
        42:41.269 SPLCLI 19564  1      C:\Dev\repos\Apps\Splunk\Splunk
        42:41.270 SPLCLI 19564  1      C:\Dev\repos\Apps\Splunk\Splunk
        42:41.270 SPLCLI 19564  1      C:\Dev\repos\Apps\Splunk\Splunk  %V
        42:41.270 SPLCLI 19564  1      C:\Dev\repos\Apps\Splunk  %W


        HKEY_CLASSES_ROOT\*\shell\testcmd -> Rt Click on a selected file
        @====> TypeSpecific
        [HKEY_CLASSES_ROOT\Directory\shell\testcmd\command]
        @="C:\Dev\repos\Apps\Splunk\Client\bin\Debug\net8.0\SplunkClient.exe" "TypeSpecific" "%S" "%H" "%L" "%D" "%V" "%W"
        args:
        22:27.292 SPLCLI 11640  1      C:\Dev\repos\Apps\Splunk\Client\bin\Debug\net8.0\SplunkClient.dll
        22:27.293 SPLCLI 11640  1      TypeSpecific
        22:27.294 SPLCLI 11640  1      1
        22:27.295 SPLCLI 11640  1      0
        22:27.296 SPLCLI 11640  1      C:\Dev\repos\Apps\Splunk\menu1.jpg
        22:27.297 SPLCLI 11640  1      C:\Dev\repos\Apps\Splunk\menu1.jpg
        22:27.297 SPLCLI 11640  1      C:\Dev\repos\Apps\Splunk\menu1.jpg  %V
        22:27.300 SPLCLI 11640  1      C:\Dev\repos\Apps\Splunk  %W

        also:
        08:40.295 SPLCLI  2976  1      C:\Dev\repos\Apps\Splunk\Client\bin\Debug\net8.0\SplunkClient.dll
        08:40.296 SPLCLI  2976  1      TypeSpecific
        08:40.296 SPLCLI  2976  1      1
        08:40.297 SPLCLI  2976  1      0
        08:40.297 SPLCLI  2976  1      C:\Users\cepth\Desktop\Clipboard_05-13-2024_01.png
        08:40.298 SPLCLI  2976  1      C:\Users\cepth\Desktop\Clipboard_05-13-2024_01.png
        08:40.298 SPLCLI  2976  1      C:\Users\cepth\Desktop\Clipboard_05-13-2024_01.png
        08:40.299 SPLCLI  2976  1      C:\Users\cepth\Desktop


        HKEY_CLASSES_ROOT\Directory\Background\shell\testcmd -> Rt Click in a dir with nothing selected
        @===> Test - Directory - Background
        [HKEY_CLASSES_ROOT\Directory\Background\shell\testcmd\command]
        @="C:\Dev\repos\Apps\Splunk\Client\bin\Debug\net8.0\SplunkClient.exe" "DirBackgShell" "%S" "%H" "%D" "%V" "%W"
        ! Blows up if I use %D or %L
        args:
        02:59.479 SPLCLI  6736  1      C:\Dev\repos\Apps\Splunk\Client\bin\Debug\net8.0\SplunkClient.dll
        02:59.490 SPLCLI  6736  1      DirBackgShell
        02:59.492 SPLCLI  6736  1      1
        02:59.492 SPLCLI  6736  1      0
        02:59.492 SPLCLI  6736  1      C:\Dev\repos\Apps  %V
        02:59.493 SPLCLI  6736  1      C:\Dev\repos\Apps  %W


        > This is from desktop with no selection.
        Computer\HKEY_CLASSES_ROOT\DesktopBackground\shell\testcmd

        ? HKEY_CLASSES_ROOT\Folder\shell\testcmd -> RtClick on dir in left pane



        */

        void Stuff()
        {
            var v = Registry.ClassesRoot;

            // Try this. You have null because in default VS reading 32x registry. You need set to 64x.
            // string subkey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\WinSAT";
            // RegistryKey localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            // string value = localKey.OpenSubKey(subkey).GetValue("PrimaryAdapterString").ToString();

            //Registry reg = new();




        }
    }

    public static class RegistryX
    {
        /// <summary>Current User Key. This key should be used as the root for all user specific settings.</summary>
        public static readonly RegistryKey CurrentUser = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

        /// <summary>Local Machine key. This key should be used as the root for all machine specific settings.</summary>
        public static readonly RegistryKey LocalMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);

        /// <summary>Classes Root Key. This is the root key of class information.</summary>
        public static readonly RegistryKey ClassesRoot = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Default);

        /// <summary>Users Root Key. This is the root of users.</summary>
        public static readonly RegistryKey Users = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Default);

        /// <summary>Performance Root Key. This is where dynamic performance data is stored on NT.</summary>
        public static readonly RegistryKey PerformanceData = RegistryKey.OpenBaseKey(RegistryHive.PerformanceData, RegistryView.Default);

        /// <summary>Current Config Root Key. This is where current configuration information is stored.</summary>
        public static readonly RegistryKey CurrentConfig = RegistryKey.OpenBaseKey(RegistryHive.CurrentConfig, RegistryView.Default);

        /// <summary>
        /// Parse a keyName and returns the basekey for it.
        /// It will also store the subkey name in the out parameter.
        /// If the keyName is not valid, we will throw ArgumentException.
        /// The return value shouldn't be null.
        /// </summary>
        private static RegistryKey GetBaseKeyFromKeyName(string keyName, out string subKeyName)
        {
            ArgumentNullException.ThrowIfNull(keyName);

            int i = keyName.IndexOf('\\');
            int length = i != -1 ? i : keyName.Length;

            // Determine the potential base key from the length.
            RegistryKey? baseKey = null;
            switch (length)
            {
                case 10: baseKey = Users; break; // HKEY_USERS
                case 17: baseKey = char.ToUpperInvariant(keyName[6]) == 'L' ? ClassesRoot : CurrentUser; break; // HKEY_C[L]ASSES_ROOT, otherwise HKEY_CURRENT_USER
                case 18: baseKey = LocalMachine; break; // HKEY_LOCAL_MACHINE
                case 19: baseKey = CurrentConfig; break; // HKEY_CURRENT_CONFIG
                case 21: baseKey = PerformanceData; break; // HKEY_PERFORMANCE_DATA
            }

            // If a potential base key was found, see if keyName actually starts with the potential base key's name.
            if (baseKey != null && keyName.StartsWith(baseKey.Name, StringComparison.OrdinalIgnoreCase))
            {
                subKeyName = (i == -1 || i == keyName.Length) ?
                    string.Empty :
                    keyName.Substring(i + 1);

                return baseKey;
            }

            // throw new ArgumentException(SR.Arg_RegInvalidKeyName, nameof(keyName));
            throw new ArgumentException("SR.Arg_RegInvalidKeyName", nameof(keyName));
        }

        public static object? GetValue(string keyName, string? valueName, object? defaultValue)
        {
            RegistryKey basekey = GetBaseKeyFromKeyName(keyName, out string subKeyName);

            using (RegistryKey? key = basekey.OpenSubKey(subKeyName))
            {
                return key?.GetValue(valueName, defaultValue);
            }
        }

        public static void SetValue(string keyName, string? valueName, object value)
        {
            SetValue(keyName, valueName, value, RegistryValueKind.Unknown);
        }

        public static void SetValue(string keyName, string? valueName, object value, RegistryValueKind valueKind)
        {
            RegistryKey basekey = GetBaseKeyFromKeyName(keyName, out string subKeyName);

            using (RegistryKey? key = basekey.CreateSubKey(subKeyName))
            {
//                Debug.Assert(key != null, "An exception should be thrown if failed!");
                key.SetValue(valueName, value, valueKind);
            }
        }
    }

}
