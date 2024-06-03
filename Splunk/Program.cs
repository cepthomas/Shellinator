using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


// TODO2 still gotta figure out the cmd <> without terminal.
// Need a replacement for cmd that does the same thing but silently. Something like https://github.com/myfreeer/hidrun


//https://stackoverflow.com/questions/836427/how-to-run-a-c-sharp-console-application-with-the-console-hidden


// powershell "start <path of batch file> -Args \"<batch file args>\" -WindowStyle Hidden"
// This can be placed in a separate batch file which, when called, will terminate immediately while your
// batch file executes in the background.
// From ' Args ' to ' \" ' can be excluded if your batch file has no arguments.
// ' -v runAs' can be added before the end quote to run your batch file as an administrator.


// TODO2 ? generic script runner - Use for all? ps, cmd, lua, py, ... or Default/builtin


namespace Splunk
{
    internal class Program
    {
        /// <summary>My logger.TODO2 really needed? Slows execution?</summary>
        static readonly Logger _logger = LogManager.CreateLogger("Splunk");

        static void Main(string[] args)
        {
            int ret = 0;

            Stopwatch sw = new();
            sw.Start();

            // Init logging for standalone application.
            string appDir = MiscUtils.GetAppDataDir("Splunk", "Ephemera");
            LogManager.MinLevelFile = LogLevel.Debug;
            LogManager.MinLevelNotif = LogLevel.Debug;
            LogManager.LogMessage += (object? _, LogMessageEventArgs e) => { Console.WriteLine($"{e.Message}"); };
            LogManager.Run($"{appDir}\\log.txt", 100000);

            Environment.ExitCode = Run(args);

            sw.Stop();
            _logger.Debug($"Elapsed msec: {sw.ElapsedMilliseconds}"); // 25
        }

        /// <summary>
        /// Do the work.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static int Run(string[] args)
        {
            int ret = 0;
            _logger.Info($"Run: {string.Join("|", args)}");

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

                // This uses Process with redirected IO so doesn't produce a console.
                ProcessStartInfo sinfo = new()
                {
                    UseShellExecute = false, //true,
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
                        _logger.Info($"Got test!!!!!!!");
                        break;

                    case "tree":
                        Process proc = new() { StartInfo = sinfo };
                        proc.Start();
                        proc.StandardInput.WriteLine($"tree /a /f \"{dir}\" | clip");

                        //check for error code
                        //proc.StandardInput.Flush();
                        //proc.StandardInput.Close();
                        // wait for the process to complete before continuing and process.ExitCode
                        proc.WaitForExit();
                        //var ret = proc.StandardOutput.ReadToEnd();
                        if (proc.ExitCode != 0 ) throw new($"Process failed: {proc.ExitCode}");
                        // There is a fundamental difference when you call WaitForExit() without a time -out, it ensures that the redirected
                        // stdout/ err have returned EOF.This makes sure that you've read all the output that was produced by the process.
                        // We can't see what "onOutput" does, but high odds that it deadlocks your program because it does something nasty
                        // like assuming that your main thread is idle when it is actually stuck in WaitForExit().
                        break;

                    /* case "tree": TODO1
                    // Uses Process with redirected io. This doesn't produce a console.
                    ProcessStartInfo sinfo = new()
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/C tree \"{dir}\" /a /f | clip";
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        // WindowStyle = ProcessWindowStyle.Hidden,
                    };

                    Process cmd = new() { StartInfo = sinfo };
                    cmd.Start();
                    cmd.StandardInput.WriteLine("tree /a /f \"C:\\Dev\\SplunkStuff\\test_dir\" | clip");
                    //cmd.StandardInput.Flush();
                    //cmd.StandardInput.Close();
                    //cmd.WaitForExit(); // wait for the process to complete before continuing and process.ExitCode
                    //var ret = cmd.StandardOutput.ReadToEnd();
                    // There is a fundamental difference when you call WaitForExit() without a time -out, it ensures that the redirected
                    // stdout/ err have returned EOF.This makes sure that you've read all the output that was produced by the process.
                    // We can't see what "onOutput" does, but high odds that it deadlocks your program because it does something nasty
                    // like assuming that your main thread is idle when it is actually stuck in WaitForExit().
                    */

                    default:
                        throw new ArgumentException($"Invalid command: {cmd}");
                }

                // var proc = new Process() { StartInfo = pinfo };
                // proc.Start();
                // //proc.WaitForExit();
                // //if (proc.ExitCode != 0) { throw new($"process exit code: {proc.ExitCode}"); }
            }
            catch (Exception ex) // handle errors
            {
                _logger.Error(ex.Message);
                ret = 1;
            }

            return ret;
        }
    }
}

