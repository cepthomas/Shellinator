using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
//using SC = Splunk.Common.Common;


//https://stackoverflow.com/questions/836427/how-to-run-a-c-sharp-console-application-with-the-console-hidden


//powershell "start <path of batch file> -Args \"<batch file args>\" -WindowStyle Hidden"
//This can be placed in a separate batch file which, when called, will terminate immediately while your batch file executes in the background.
//From ' Args ' to ' \" ' can be excluded if your batch file has no arguments.
//' -v runAs' can be added before the end quote to run your batch file as an administrator.



namespace Splunk
{
    internal class Program
    {
        /// <summary>My logger.</summary>
//        static readonly Logger _logger = LogManager.CreateLogger("Splunk");

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

            ret = Run(args);

            sw.Stop();

//            _logger.Debug($"Elapsed msec: {sw.ElapsedMilliseconds}"); // 25

            Environment.ExitCode = ret;
        }

        /// <summary>
        /// Do the work.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static int Run(string[] args)
        {
            int ret = 0;
//            _logger.Info($"Run: {string.Join("|", args)}");

            // TODO2 ? generic script runner - Use for all? ps, cmd, lua, py, ... or Default/builtin

            try
            {
                // Process the command string.
                if (args.Length != 2) { throw new($"invalid command format"); }
                var cmd = args[0];
                var path = args[1];

                // Check for valid path arg.
                if (!Path.Exists(path)) { throw new($"invalid path: {path}"); }
                FileAttributes attr = File.GetAttributes(path);
                var dir = attr.HasFlag(FileAttributes.Directory) ? path : Path.GetDirectoryName(path)!;

                // Check for valid command and execute it.
                ProcessStartInfo pinfo = new()
                {
                    UseShellExecute = false, //true,
                    CreateNoWindow = true,
                    WorkingDirectory = dir,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    //RedirectStandardInput = true,
                    //RedirectStandardOutput = true,
                };

                switch (cmd)
                {
                    case "cmder":
                        // Open a new explorer window at the dir selected in the first one.
                        // Locate it on one side or other of the first, same size.
                        // TODO2 option for full screen?

                        //https://stackoverflow.com/questions/1190423/using-setwindowpos-in-c-sharp-to-move-windows-around

                        pinfo.FileName = "explorer";
                        pinfo.Arguments = $"{dir}";
                        break;

                    case "newtab":
                        // Open a new explorer tab in current window at the dir selected in the first one.
                        // (explorer middle button?) ctrl-T opens selected in new tab
                        //int l = (int)MouseButtons.Left; // 0x00100000
                        //int m = (int)MouseButtons.Middle; // 0x00400000
                        //int r = (int)MouseButtons.Right; // 0x00200000

                        // Or something like https://github.com/tariibaba/WinENFET/blob/main/src (autohotkey)./win-e.ahk

                        pinfo.FileName = "cmd";
                        pinfo.Arguments = $"/c echo !!newtab!! {DateTime.Now.Millisecond} | clip";
                        break;

                    //case "tree": // direct => cmd /c tree /a /f "%V" | clip
                    //    pinfo.FileName = "cmd";
                    //    pinfo.Arguments = $"/C tree \"{dir}\" /a /f | clip";
                    //    break;

                    //case "openst": // direct
                    //    pinfo.FileName = "subl";
                    //    pinfo.Arguments = $"-n \"{dir}\"";
                    //    break;

                    //case "find": // direct
                    //    pinfo.FileName = "everything";
                    //    pinfo.Arguments = $"-parent \"{dir}\"";
                    //    pinfo.WorkingDirectory = @"C:\Program Files\Everything";
                    //    break;

                    default:
                        throw new ArgumentException($"Invalid command: {cmd}");
                }

                var proc = new Process() { StartInfo = pinfo };
                proc.Start();
                //proc.WaitForExit();
                //if (proc.ExitCode != 0) { throw new($"process exit code: {proc.ExitCode}"); }
            }
            catch (Exception ex) // handle errors
            {
//                _logger.Error(ex.Message);
                ret = 1;
            }

            return ret;
        }
    }
}

