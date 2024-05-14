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
            filTreeX.FilterExts = [".txt", ".ntr", ".md", ".xml", ".cs", ".py"];
            filTreeX.IgnoreDirs = [".vs", ".git", "bin", "obj", "lib"];
            filTreeX.RootDirs =
            [
                @"C:\Users\cepth\AppData\Roaming\Sublime Text\Packages\Notr",
                @"C:\Users\cepth\OneDrive\OneDriveDocuments\notes"
            ];
            //filTree.RecentFiles = new()
            //{
            //    @"C:\Dev\repos\repos_common\audio_file_info.txt",
            //    @"C:\Dev\repos\repos_common\build.txt"
            //};
            filTreeX.SplitterPosition = 40;
            filTreeX.SingleClickSelect = false;
            filTreeX.InitTree();
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
    }
}
