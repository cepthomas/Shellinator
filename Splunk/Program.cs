using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Splunk.Common;
using NM = Splunk.Common.NativeMethods;
using SU = Splunk.Common.ShellUtils;
using System.Linq;
using System.Drawing;
using System.ComponentModel;
using System.Text;
using System.Runtime.InteropServices;

// TODO1 plugin model?
// TODO1 test from UI

namespace Splunk
{
    public class Program
    {
        #region Fields - public for debugging
        /// <summary>Crude debugging without spinning up a console or logger.</summary>
        public static readonly List<string> _debug = [];

        /// <summary>Result of command execution.</summary>
        public static string _stdout = "";

        /// <summary>Result of command execution.</summary>
        public static string _stderr = "";
        #endregion

        /// <summary>Where it all began.</summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            _debug.Add($"{DateTime.Now} Splunk [{string.Join(" ", args)}]");

            // I'm in charge of the pixels.
            NM.SetProcessDPIAware();
            string appDir = MiscUtils.GetAppDataDir("Splunk", "Ephemera");

            Stopwatch sw = new();
            sw.Start();

            // Execute.
            var code = Run([.. args]);

            // What happened?
            switch (code)
            {
                case 0:
                    // Everything ok.
                    PackageResult();
                    Environment.ExitCode = 0;
                    break;

                case null:
                    // Splunk internal error.
                    PackageResult();
                    Environment.ExitCode = 1;
                    break;

                default:
                    // Process execute code.
                    PackageResult(new Win32Exception((int)code).Message);
                    Environment.ExitCode = 2;
                    break;
            }

            sw.Stop();
            _debug.Add($"Exit:{Environment.ExitCode} Msec:{sw.ElapsedMilliseconds}");
            File.AppendAllLines(Path.Join(appDir, "debug.txt"), _debug);
        }

        /// <summary>Do the work.</summary>
        /// <param name="args"></param>
        /// <returns>Process return code or null if internal error.</returns>
        public static int? Run(List<string> args)
        {
            int? ret = 0;

            //Debugger.Break();
            //Use ::DebugBreak() in the context handler to cause a break into the debugger.

            try
            {
                // Process the args. Splunk.exe "id" "path"
                if (args.Count != 2) { throw new($"Invalid command line format"); }
                var id = Environment.ExpandEnvironmentVariables(args[0]);
                var path = Environment.ExpandEnvironmentVariables(args[1]);

                // Check for valid path.
                if (!Path.Exists(path)) { throw new($"Invalid path: {path}"); }

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
                        ret = ExecuteCommand("cmd", wdir, $"tree /a /f \"{wdir}\"");
                        break;

                    case "exec":
                        if (!isdir)
                        {
                            var ext = Path.GetExtension(path);
                            ret = ext switch
                            {
                                ".cmd" or ".bat" => ExecuteCommand("cmd", wdir, $"/c /q \"{path}\""),
                                ".ps1" => ExecuteCommand("powershell", wdir, $"-executionpolicy bypass -File \"{path}\""),
                                ".lua" => ExecuteCommand("lua", wdir, $"\"{path}\""),
                                ".py" => ExecuteCommand("python", wdir, $"\"{path}\""),
                                _ => ExecuteCommand("cmd", wdir, $"\"{path}\"") // Others - default just open.
                            };
                        }
                        else
                        {
                            // ignore selection of dir
                        }
                        break;

                    case "test_deskbg":
                        _debug.Add($"Got test_deskbg");
                        //NM.MessageBox(IntPtr.Zero, "ExplorerContext.DeskBg", "Greetings From", 0);
                        break;

                    case "test_folder":
                        _debug.Add($"Got test_folder");
                        //NM.MessageBox(IntPtr.Zero, "ExplorerContext.Folder", "Greetings From", 0);
                        break;

                    default:
                        throw new ArgumentException($"Invalid id: {id}");
                }
            }
            catch (Exception ex)
            {
                // Splunk internal error.
                _debug.Add($"ERROR:{DateTime.Now} {ex.Message}");
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
        /// <param name="cmd"></param>
        /// <returns></returns>
        static int ExecuteCommand(string exe, string wdir, string cmd)
        {
            ProcessStartInfo pinfo = new()
            {
                FileName = exe,
                UseShellExecute = false,
                CreateNoWindow = true,
// TODO1 needed?
                WorkingDirectory = wdir,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using (Process proc = new() { StartInfo = pinfo })
            {
                proc.Start();
                proc.StandardInput.WriteLine($"{exe} {cmd}");
                proc.StandardInput.WriteLine($"exit");
                proc.WaitForExit();

                _stdout = proc.StandardOutput.ReadToEnd();
                _stderr = proc.StandardError.ReadToEnd();

                return proc.ExitCode;
            }
        }

        /// <summary>
        /// Gather what happened into something useful to the caller.
        /// </summary>
        /// <param name="extra"></param>
        static void PackageResult(string extra = "")
        {
            StringBuilder sb = new();
            if (extra.Length > 0 )
            {
                sb.AppendLine("========== extra ==========");
                sb.AppendLine(extra);
            }

            if (_stderr.Length > 0)
            {
                sb.AppendLine("========== stderr ==========");
                sb.AppendLine(_stderr);
            }

            if (_stdout.Length > 0)
            {
                sb.AppendLine("========== stdout ==========");
                sb.AppendLine(_stdout);
            }
            sb.Append('\0');

            // To clipboard or file?
            string nullTerminatedStr = sb.ToString();// + '\0';
            byte[] strBytes = Encoding.Unicode.GetBytes(nullTerminatedStr);
            IntPtr hglobal = Marshal.AllocHGlobal(strBytes.Length);
            Marshal.Copy(strBytes, 0, hglobal, strBytes.Length);
            NM.OpenClipboard(IntPtr.Zero);
            //NM.EmptyClipboard();
            NM.SetClipboardData((int)NM.ClipboardFormats.CF_TEXT, hglobal);
            NM.CloseClipboard();
            Marshal.FreeHGlobal(hglobal);
        }
    }
}
