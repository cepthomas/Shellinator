using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Ipc = Ephemera.NBagOfTricks.SimpleIpc;


namespace Splunk
{
    public partial class MainForm : Form
    {
        //const string TS_FORMAT = @"mm\:ss\.fff";
        const string PIPE_NAME = "058F684D-AF82-4FE5-BD1E-9FD031FE28CF";
        const string LOGFILE = @"C:\Dev\repos\Splunk\test_ipc_log.txt";
        readonly Ipc.MpLog _log;// = new(LOGFILE, "SPLUNK");

        Ipc.Server server;

        public MainForm()
        {
            InitializeComponent();

            if (!File.Exists(LOGFILE))
            {
                File.WriteAllText(LOGFILE, $"===== New log file ===={Environment.NewLine}");
            }
            _log = new(LOGFILE, "SPLUNK");

            _log.Write("Hello from UI");

            ///// Text control.
            tvInfo.MatchColors.Add("ERR ", Color.Purple);
            // tvInfo.MatchColors.Add("55", Color.Green);
            tvInfo.BackColor = Color.Cornsilk;
            tvInfo.Prompt = ">";

            ///// FilTree.
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
            //            filTree.FileSelected += (object? sender, string fn) => { Tell($"Selected file: {fn}"); _settings.UpdateMru(fn); };


            //case "SingleClickSelect":
            //    filTree.SingleClickSelect = _settings.SingleClickSelect;
            //    break;

            //case "SplitterPosition":
            //    filTree.SplitterPosition = _settings.SplitterPosition;
            //    break;


            // Run server
            //using Ipc.Server server = new(PIPE_NAME, LOGFILE);
            server = new(PIPE_NAME, LOGFILE);
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
            var stat = e.Error ? "ERR " : "";

            tvInfo.AppendLine($"{stat} {e.Message}");
        }

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
