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
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            // ApplicationConfiguration.Initialize();

            NM.SetProcessDPIAware();

            Stopwatch sw = new();
            sw.Start();
            string appDir = MiscUtils.GetAppDataDir("Splunk", "Ephemera");

            _debug.Add($"=== Run {DateTime.Now} [{string.Join(" ", args)}] ===");
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
                        DoCmder(path);
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

                    case "test":
                        _debug.Add($"Got test!!!!!!!");
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


        static void DoCmder(string targetDir)
        {
            var fgHandle = NM.GetForegroundWindow(); // -> left pane
            WindowInfo fginfo = SU.GetWindowInfo(fgHandle);
            _debug.Add($"fginfo {fginfo}");

            //SU.GetAppWindows("explorer").ForEach(w => _debug.Add($">>> {w}"));

            //// Get the current explorer path. Note: could also use the %W arg.
            //string? currentPath = Path.GetDirectoryName(targetDir) ??
            //    throw new InvalidOperationException($"Couldn't get path for [{targetDir}]");

            //_debug.Add($"targetDir:[{targetDir}] currentPath:[{currentPath}]");

            //// Locate the originator.
            //var wins = SU.GetAppWindows("explorer");

            //WindowInfo? currentExplorer = wins.Where(w => w.Title == currentPath).FirstOrDefault() ??
            //    throw new InvalidOperationException($"Couldn't get originator explorer for {targetDir}");

            //// Create a copy of the first explorer -> left pane.
            //NM.ShellExecute(IntPtr.Zero, "explore", currentPath, IntPtr.Zero, IntPtr.Zero, (int)NM.ShowCommands.SW_NORMAL);

            // Create the new explorer -> right pane.
            NM.ShellExecute(IntPtr.Zero, "explore", targetDir, IntPtr.Zero, IntPtr.Zero, (int)NM.ShowCommands.SW_NORMAL);

            // Wait for new window to be created. This is klunky...
            int tries = 0;
            //WindowInfo? leftPane = null;
            WindowInfo? rightPane = null;
            for (tries = 0; tries < 20 && rightPane is null; tries++) // ~4
            {
                System.Threading.Thread.Sleep(50);

                // Locate the new explorer window.

                var wins = SU.GetAppWindows("explorer");
                rightPane = wins.Where(w => w.Title == targetDir).FirstOrDefault();



                //foreach (var win in wins)
                //{
                //   // if (win.Title == currentPath && win.Handle != currentExplorer.Handle) { leftPane = win; }
                //    if (win.Title == targetDir) { rightPane = win; }
                //}
            }

            _debug.Add($"tries:{tries}");

            //if (leftPane is null) throw new InvalidOperationException($"Couldn't create left pane for [{currentPath}]");
            if (rightPane is null) throw new InvalidOperationException($"Couldn't create right pane for [{targetDir}]");

            //_debug.Add($"currentExplorer:{currentExplorer.Handle} leftPane:{leftPane.Handle} rightPane:{rightPane.Handle}");

            // Relocate the windows to taste.  For 1920x1080 display.
            IntPtr swHandle = NM.GetShellWindow();
            WindowInfo desktop = SU.GetWindowInfo(swHandle);
            //_debug.Add($"swinfo {swinfo}");
            //swinfo Title[Program Manager] Geometry[X:0 Y:0 W:1920 H:1080] IsVisible[True] Handle[65868] Pid[6556]
            //public NM.RECT DisplayRectangle { get { return NativeInfo.rcWindow; } }

            //swinfo.DisplayRectangle;
            Rectangle rectangle = desktop.DisplayRectangle;

            // TODO2 Get size from settings or desktop.
            int w = 800;
            int h = 800;
            int t = 50;
            int l = 50;

            _ = NM.MoveWindow(fgHandle, l, t, w, h, true);
            NM.SetForegroundWindow(fgHandle);

            _ = NM.MoveWindow(rightPane.Handle, l + w, t, w, h, true);
            NM.SetForegroundWindow(rightPane.Handle);

            //SU.GetAppWindows("explorer").ForEach(w => _debug.Add($"<<< {w}"));
        }
    }
}

