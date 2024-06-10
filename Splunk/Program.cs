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


namespace Splunk
{
    internal class Program
    {
        /// <summary>Crude debugging without a console or logger.</summary>
        static readonly List<string> _debug = [];

        /// <summary>Where it all began.</summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // I'm in charge of the pixels.
            NM.SetProcessDPIAware();
            string appDir = MiscUtils.GetAppDataDir("Splunk", "Ephemera");

            Stopwatch sw = new();
            sw.Start();
            _debug.Add($"========== Run {DateTime.Now} [{string.Join(" ", args)}]");
            Environment.ExitCode = Run(args);
            sw.Stop();
            _debug.Add($"Exit {Environment.ExitCode} {sw.ElapsedMilliseconds} msec");
            File.AppendAllLines(Path.Join(appDir, "debug.txt"), _debug);
        }

        /// <summary>Do the work.</summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static int Run(string[] args)
        {
            int ret = 0;

            try
            {
                // Process the args.
                if (args.Length != 2) { throw new($"Invalid command format"); }
                var cmd = args[0];
                var path = args[1];
                // Check for valid path.
                if (!Path.Exists(path)) { throw new($"Invalid path: {path}"); }
                FileAttributes attr = File.GetAttributes(path);
                var dir = attr.HasFlag(FileAttributes.Directory) ? path : Path.GetDirectoryName(path)!;

                ProcessStartInfo sinfo = new()
                {
                    FileName = "cmd",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = dir,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                };

                switch (cmd)
                {
                    case "cmder":
                        var fgHandle = NM.GetForegroundWindow(); // -> left pane
                        WindowInfo fginfo = SU.GetWindowInfo(fgHandle);
                        // New explorer -> right pane.
                        NM.ShellExecute(IntPtr.Zero, "explore", path, IntPtr.Zero, IntPtr.Zero, (int)NM.ShowCommands.SW_NORMAL);
                        // Locate the new explorer window. Wait for it to be created. This is a bit klunky...
                        int tries = 0;
                        WindowInfo? rightPane = null;
                        for (tries = 0; tries < 20 && rightPane is null; tries++) // ~4
                        {
                            System.Threading.Thread.Sleep(50);
                            var wins = SU.GetAppWindows("explorer");
                            rightPane = wins.Where(w => w.Title == path).FirstOrDefault();
                        }
                        if (rightPane is null) throw new InvalidOperationException($"Couldn't create right pane for [{path}]");

                        // Relocate the windows to taste. TODO Get size from settings or desktop or ???.
                        WindowInfo desktop = SU.GetWindowInfo(NM.GetShellWindow());
                        int w = 800, h = 800, t = 50, l = 50; // Hardcode for 1920x1080 display.
                        _ = NM.MoveWindow(fgHandle, l, t, w, h, true);
                        NM.SetForegroundWindow(fgHandle);
                        _ = NM.MoveWindow(rightPane.Handle, l + w, t, w, h, true);
                        NM.SetForegroundWindow(rightPane.Handle);
                        break;

                    case "tree":
                        using (Process proc = new() { StartInfo = sinfo })
                        {
                            proc.Start();
                            proc.StandardInput.WriteLine($"tree /a /f \"{dir}\" | clip");
                            proc.StandardInput.WriteLine($"exit");
                            proc.WaitForExit();
                            if (proc.ExitCode != 0) throw new($"Process failed: {proc.ExitCode}");
                        }
                        break;

                    default:
                        throw new ArgumentException($"Invalid command: {cmd}");
                }
            }
            catch (Exception ex)
            {
                _debug.Add($"Error [{ex.Message}]");
                ret = 1;
            }

            return ret;
        }
    }
}

