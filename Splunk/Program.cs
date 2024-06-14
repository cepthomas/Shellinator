using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using System.Linq;
using System.Drawing;
using System.ComponentModel;
using System.Text;
using System.Runtime.InteropServices;
using System.Text.Json;
using Splunk.Common;
using NM = Splunk.Common.NativeMethods;
using SU = Splunk.Common.ShellUtils;


namespace Splunk
{
    public class Program
    {
        #region Fields - public for debugging
        /// <summary>Result of command execution.</summary>
        public static string _stdout = "";  // TODO1 stdout/stderr to clipboard and/or logfile?

        /// <summary>Result of command execution.</summary>
        public static string _stderr = "";

        /// <summary>Log file name or clipboard if null.</summary>
        static string? _logFile = Path.Join(MiscUtils.GetAppDataDir("Splunk", "Ephemera"), "splunk.txt");
        #endregion

        /// <summary>Where it all began.</summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Log($"Splunk cl args:{string.Join(" ", args)}");

            // I'm in charge of the pixels.
            NM.SetProcessDPIAware();

            Stopwatch sw = new();
            sw.Start();

            // Execute.
            var code = Run([.. args]);

            // What happened?
            switch (code)
            {
                case 0:
                    // Everything ok.
                    Log(_stdout.Length > 0 ? $"OK{Environment.NewLine}{_stdout}" : "OK");
                    Environment.ExitCode = 0;
                    break;

                case null:
                    // Splunk internal error.
                    Log($"ERROR splunk{Environment.NewLine}{_stderr}");
                    Environment.ExitCode = 1;
                    break;

                default:
                    // Process exit code.
                    Log($"ERROR process {(int)code}:{new Win32Exception((int)code).Message}{Environment.NewLine}{_stderr}");
                    Environment.ExitCode = 2;
                    break;
            }

            sw.Stop();
            Log($"Exit code:{Environment.ExitCode} msec:{sw.ElapsedMilliseconds}");
        }

        /// <summary>Do the work.</summary>
        /// <param name="args"></param>
        /// <returns>Process return code or null if internal error.</returns>
        public static int? Run(List<string> args)
        {
            int? ret = 0;
            //Debugger.Break();

            try
            {
                // Process the args. Splunk.exe "id" "path"
                if (args.Count != 2) { throw new($"Invalid command line format"); }
                var id = Environment.ExpandEnvironmentVariables(args[0]);
                var path = Environment.ExpandEnvironmentVariables(args[1]);

                // Check for valid path.
                if (path.StartsWith("::")) { throw new($"Can't use system folders e.g. Home"); }
                else if (!Path.Exists(path)) { throw new($"Invalid path [{path}]"); }

                // Final details.
                FileAttributes attr = File.GetAttributes(path);
                var wdir = attr.HasFlag(FileAttributes.Directory) ? path : Path.GetDirectoryName(path)!;
                var isdir = attr.HasFlag(FileAttributes.Directory);

                switch (id)
                {
                    case "cmder":
                        var fgHandle = NM.GetForegroundWindow(); // -> left pane
                        WindowInfo fginfo = SU.GetWindowInfo(fgHandle);

                        // New explorer -> right pane.
                        NM.ShellExecute(IntPtr.Zero, "explore", path, IntPtr.Zero, IntPtr.Zero, (int)NM.ShowCommands.SW_NORMAL);

                        // Locate the new explorer window. Wait for it to be created. This is a bit klunky but there does not appear to be a more direct method.
                        int tries = 0; // ~4
                        WindowInfo? rightPane = null;
                        for (tries = 0; tries < 20 && rightPane is null; tries++)
                        {
                            System.Threading.Thread.Sleep(50);
                            var wins = SU.GetAppWindows("explorer");
                            rightPane = wins.Where(w => w.Title == path).FirstOrDefault();
                        }
                        if (rightPane is null) throw new InvalidOperationException($"Couldn't create right pane for [{path}]");

                        // Relocate/resize the windows to fit available real estate.
                        WindowInfo desktop = SU.GetWindowInfo(NM.GetShellWindow());
                        int w = desktop.DisplayRectangle.Width * 45 / 100;
                        int h = desktop.DisplayRectangle.Height * 80 / 100;
                        int t = 50, l = 50;
                        NM.MoveWindow(fgHandle, l, t, w, h, true);
                        NM.SetForegroundWindow(fgHandle);
                        l += w;
                        NM.MoveWindow(rightPane.Handle, l, t, w, h, true);
                        NM.SetForegroundWindow(rightPane.Handle);
                        break;

                    case "tree":
                        ret = ExecuteCommand("cmd", wdir, $"/c tree /a /f \"{wdir}\" | clip");
                        break;

                    case "exec":
                        if (!isdir)
                        {
                            var ext = Path.GetExtension(path);
                            ret = ext switch
                            {
                                ".cmd" or ".bat" => ExecuteCommand("cmd", wdir, $"/c \"{path}\""),
                                ".ps1" => ExecuteCommand("powershell", wdir, $"-executionpolicy bypass -File \"{path}\""),
                                ".lua" => ExecuteCommand("lua", wdir, $"\"{path}\""),
                                ".py" => ExecuteCommand("python", wdir, $"\"{path}\""),
                                _ => ExecuteCommand("cmd", wdir, $"/c \"{path}\"") // default just open.
                            };
                        }
                        else
                        {
                            // ignore selection of dir
                        }
                        break;

                    case "test_deskbg":
                        Log($"!!! Got test_deskbg");
                        NM.MessageBox(IntPtr.Zero, $"!!! Got test_deskbg: {path}", "Debug", 0);
                        break;

                    case "test_folder":
                        Log($"!!! Got test_folder");
                        NM.MessageBox(IntPtr.Zero, "!!! Got test_folder: {path}", "Debug", 0);
                        break;

                    default:
                        throw new ArgumentException($"Invalid id: {id}");
                }
            }
            catch (Exception ex)
            {
                // Splunk internal error.
                _stderr = $"{ex.Message}{Environment.NewLine}{ex.StackTrace}";
                ret = null;
            }

            return ret;
        }

        /// <summary>
        /// Generic command executor with hidden console.
        /// </summary>
        /// <param name="exe"></param>
        /// <param name="wdir"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        static int ExecuteCommand(string exe, string wdir, string args)
        {
            //Log($"DEBUG args:{args}");

            ProcessStartInfo pinfo = new()
            {
                FileName = exe,
                Arguments = args,
                //WorkingDirectory = wdir, // TODO1 needed?
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                //RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using Process proc = new() { StartInfo = pinfo };
            proc.Start();
            proc.WaitForExit();
            _stdout = proc.StandardOutput.ReadToEnd();
            _stderr = proc.StandardError.ReadToEnd();

            return proc.ExitCode;
        }

        /// <summary>Crude debugging without spinning up a console or logger.</summary>
        static void Log(string msg)
        {
            // TODO1 try real logging. simple style durations:
            //Splunk cl args: exec C:\Dev\repos\Apps\Splunk\Test\go.cmd Exit code: 0 msec: 46
            //Splunk cl args: exec C:\Dev\repos\Apps\Splunk\Test\go.lua Exit code: 0 msec: 27
            //Splunk cl args: tree C:\Dev\repos\Apps\Splunk\Test\bin Exit code: 0 msec: 53
            //Splunk cl args: exec C:\Dev\repos\Apps\Splunk\Splunk\bin\Debug\net8.0\Ephemera.NBagOfTricks.xml Exit code: 0 msec: 215
            //Splunk cl args: cmder C:\Dev\repos\Apps\Splunk\Splunk\bin Exit code: 0 msec: 408
            //Splunk cl args: exec C:\Dev\repos\Apps\Splunk\Test\dummy.txt Exit code: 0 msec: 212

            if (_logFile is not null)
            {
                File.AppendAllText(_logFile, $"{DateTime.Now:yyyy'-'MM'-'dd HH':'mm':'ss.fff} {msg}{Environment.NewLine}");
            }
        }
    }
}
