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
        public static string _stdout = "";

        /// <summary>Result of command execution.</summary>
        public static string _stderr = "";

        /// <summary>Log file name.</summary>
        static string _logFileName = Path.Join(MiscUtils.GetAppDataDir("Splunk", "Ephemera"), "splunk.txt");

        /// <summary>Stdio goes to clipboard.</summary>
        static bool _useClipboard = true;
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

            sw.Stop();
            Log($"Exit code:{Environment.ExitCode} msec:{sw.ElapsedMilliseconds}");

            // What happened?
            string sout = ""; // TODO
            switch (code)
            {
                case 0:
                    // Everything ok.
                    sout = _stdout;
                    Log("OK");
                    Environment.ExitCode = 0;
                    break;

                case null:
                    // Splunk internal error.
                    sout = $"ERROR splunk{Environment.NewLine}{_stderr}";
                    Log(sout);
                    Environment.ExitCode = 1;
                    break;

                default:
                    // Process error.
                    sout = $"ERROR process {(int)code}:{new Win32Exception((int)code).Message}{Environment.NewLine}{_stderr}";
                    Log(sout);
                    Environment.ExitCode = 2;
                    break;
            }

            if (_useClipboard)
            {
                Clipboard.SetText(sout);
            }

            // Before we end, manage log size. TODO
            FileInfo fi = new(_logFileName);
            if (fi.Exists && fi.Length > 10000)
            {
                //Open both the input file and a new output file(as a TextReader / TextWriter, e.g.with File.OpenText and File.CreateText)
                //Read a line(TextReader.ReadLine) - if you don't want to delete it, write it to the output file (TextWriter.WriteLine)
                //When you've read all the lines, close both the reader and the writer (if you use using statements for both,
                //this will happen automatically)
            }
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
                WorkingDirectory = wdir, // needed?
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
            if (_logFileName is not null)
            {
                File.AppendAllText(_logFileName, $"{DateTime.Now:yyyy'-'MM'-'dd HH':'mm':'ss.fff} {msg}{Environment.NewLine}");
            }
        }
    }
}
