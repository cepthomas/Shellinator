using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Linq;
using System.Windows.Forms;


namespace ShellinatorXXX
{

    /// <summary>Convenience container.</summary>
    /// <param name="Code">Return code</param>
    /// <param name="Stdout">stdout if any</param>
    /// <param name="Stderr">stderr if any</param>
    public readonly record struct ExecResult(int Code, string Stdout, string Stderr);

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Log("Hello, World!");

            // To not show console, make project     <OutputType>WinExe</OutputType>


            // System commands need to be invoked by cmd.
            //   used to execute command prompt (cmd.exe) commands or applications
            //   Use /C to execute the command and then terminate the command prompt,
            //     or /K to execute the command and keep the command prompt open.
            //   set the WorkingDirectory: This specifies the directory where the command will be executed.


            // This works:
            //var res = ExecuteCommand("cmd", ["/C", "dir", @"C:\Dev\Apps\Treex\Test"]);

            // This fails with An error occurred trying to start process 'dir' with working directory '...'. The system cannot find the file specified.
            //var res = ExecuteCommand("dir", [@"C:\Dev\Apps\Treex\Test"]);

            // This works:
            //var res = ExecuteCommand("cmd", ["/C", "treex", @"C:\Dev\Apps\Treex\Test"]);

            // This works:
            //var res = ExecuteCommand("treex", [@"C:\Dev\Apps\Treex\Test"]);

            // This works:
            var res = ExecuteCommand("cmd", ["/C", "treex", @"C:\Dev\Apps\Treex\Test"]);

            var stdout = res.Stdout == "" ? "none" : res.Stdout;
            var stderr = res.Stderr == "" ? "none" : res.Stderr;

            Log($"code: {res.Code}");
            Log($"stdout:");
            Log($"{stdout}");
            Log($"stderr:");
            Log($"{stderr}");


            Clipboard.SetText(stdout);



        }

        /// <summary>
        /// Generic command executor. Suppresses console window creation.
        /// </summary>
        /// <param name="exe"></param>
        /// <param name="args"></param>
        /// <returns>Result code, stdout, stderr</returns>
        public ExecResult ExecuteCommand(string exe, List<string> args)
        {
            //var sargs = new List<string>();
            //args.ForEach(a =>  sargs.Add($"[{a}]"));
            Log($"ExecuteCommand() exe:[{exe}] args:[{string.Join(" ", args)}]");

            // CreateNoWindow = true ensures no command window pops up.
            // UseShellExecute = false is required for hiding the window and enabling advanced options like redirection.


            // Using properties.
            // ProcessStartInfo pinfo = new()
            // {
            //     FileName = exe,
            //     Arguments = string.Join(" ", args),
            //     //WorkingDirectory = wdir, // needed?
            //     UseShellExecute = false,
            //     CreateNoWindow = true,
            //     WindowStyle = ProcessWindowStyle.Hidden,
            //     //RedirectStandardInput = true,
            //     RedirectStandardOutput = true,
            //     RedirectStandardError = true,
            // };

            // Using args.
            ProcessStartInfo pinfo = new(exe, args)
            {
                //WorkingDirectory = @"C:\Dev\Apps", // needed? Don't think so normally...
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                //RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,

                //EnvironmentVariables["MY_VAR"] = "Hello, Environment!"

            };

            //Sometimes, a process requires specific environmental variables to run correctly.
            pinfo.EnvironmentVariables["MY_VAR"] = "Hello, Environment!";

            using Process proc = new() { StartInfo = pinfo };

            proc.Exited += (sender, e) =>
            {
                Log("Process has exited.");
            };

            Log("Start process...");
            proc.Start();

            // TIL: To avoid deadlocks, always read the output stream first and then wait.
            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();

            Log("Waiting for process to exit...");
            proc.WaitForExit();

            Log("All done.");

            return new(proc.ExitCode, stdout, stderr);
        }


        /// <summary>Simple logging.</summary>
        void Log(string msg, bool show = false)
        {
            TextOut.AppendText($"{msg}{Environment.NewLine}");
        }


    }
}
