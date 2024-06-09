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


// Set OutputType to WinExe in order to prevent a visible console.

// TODO2 ? generic script runner - Use for all? ps, cmd, lua, py, ... or Default/builtin


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
            Stopwatch sw = new();
            sw.Start();
            string appDir = MiscUtils.GetAppDataDir("Splunk", "Ephemera");
            _debug.Add($"=== Run {DateTime.Now} [{string.Join(" ", args)}] ===");
            Environment.ExitCode = Run(args);
            sw.Stop();
            _debug.Add($"Exit {Environment.ExitCode} {sw.ElapsedMilliseconds}");
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

                // Check for valid path arg.
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

                    case "test":
                        _debug.Add($"Got test!!!!!!!");
                        break;

                    case "tree":
                        using (Process proc = new() { StartInfo = sinfo })
                        {
                            proc.Start();
                            proc.StandardInput.WriteLine($"tree /a /f \"{dir}\" | clip");
                            proc.StandardInput.WriteLine($"exit");
                            //proc.StandardInput.Flush();
                            //proc.StandardInput.Close();
                            proc.WaitForExit();
                            //var ret = proc.StandardOutput.ReadToEnd();
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


        static void DoCmder(string targetDir)
        {
            try
            {
                // TODO2 Option for custom sizes, full screen?
                // TODO1 handle errors.

                //var targetDirXXX = @"C:\Dev\SplunkStuff"; // TODO1 fake from cmd line path - the rt click dir

                // Get the current explorer path. Note: could also use the %W arg.
                string? currentPath = Path.GetDirectoryName(targetDir) ?? throw new InvalidOperationException($"Couldn't get path for {targetDir}");

                // Locate the originator.
                var wins = SU.GetAppWindows("explorer");

                WindowInfo? currentExplorer = wins.Where(w => w.Title == currentPath).FirstOrDefault() ?? throw new InvalidOperationException($"Couldn't get originator explorer for {targetDir}");

                // Create a copy of the first explorer -> left pane.
                NM.ShellExecute(IntPtr.Zero, "explore", currentPath, IntPtr.Zero, IntPtr.Zero, (int)NM.ShowCommands.SW_NORMAL);

                // Create the new explorer -> right pane.
                NM.ShellExecute(IntPtr.Zero, "explore", targetDir, IntPtr.Zero, IntPtr.Zero, (int)NM.ShowCommands.SW_NORMAL);

                // Wait for new windows to be created.
                int tries = 0;
                WindowInfo? leftPane = null;
                WindowInfo? rightPane = null;
                for (tries = 0; tries < 10 && leftPane is null && rightPane is null; tries++) // ~4
                {
                    System.Threading.Thread.Sleep(50);

                    // Locate the two new explorer windows.
                    wins = SU.GetAppWindows("explorer");
                    foreach (var win in wins)
                    {
                        if (win.Title == currentPath && win.Handle != currentExplorer.Handle) { leftPane = win; }
                        if (win.Title == targetDir) { rightPane = win; }
                    }
                }

                if (leftPane is null) throw new InvalidOperationException($"Couldn't create left pane for {currentPath}");
                if (rightPane is null) throw new InvalidOperationException($"Couldn't create right pane for {targetDir}");

                _debug.Add($"tries:{tries} currentExplorer:{currentExplorer.Handle} leftPane:{leftPane.Handle} rightPane:{rightPane.Handle}");

                // Relocate the windows to taste.  For 1920x1080 display.
                int w = 800;
                int h = 900;
                int t = 50;
                int l = 50;

                //> Title[C:\Dev] Geometry[X: 63 Y: 63 W: 1000 H: 1102] IsVisible[True] Handle[592160] Pid[10012]
                //> Title[C:\Dev\_leftovers] Geometry[X: 1063 Y: 63 W: 1000 H: 1102] IsVisible[True] Handle[4655796] Pid[10012]
                //> Title[C:\Dev] Geometry[X: 210 Y: 64 W: 1402 H: 900] IsVisible[True] Handle[526996] Pid[10012]

                _ = NM.MoveWindow(leftPane.Handle, l, t, w, h, true);
                NM.SetForegroundWindow(leftPane.Handle);

                _ = NM.MoveWindow(rightPane.Handle, l + w, t, w, h, true);
                NM.SetForegroundWindow(rightPane.Handle);
            }
            catch (Exception ex)
            {
                _debug.Add(ex.Message);
            }
        }
    }
}

