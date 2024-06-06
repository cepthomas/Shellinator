using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


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
    }
}

