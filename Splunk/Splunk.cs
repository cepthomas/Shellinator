using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


namespace Splunk
{
    /// <summary>The worker.</summary>
    public class Splunk
    {
        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("Splunk");

        /// <summary>
        /// Do the work.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public int Run(string[] args)
        {
            int ret = 0;
            _logger.Info($"Run: {string.Join("|", args)}");

            // TODO1 still gotta figure out the cmd <> without terminal. See what python does.
            // case "tree": // direct => cmd /c tree /a /f "%V" | clip
            // still flashes, ? Try ShellExecuteEx, setting nShow=SW_HIDE. https://learn.microsoft.com/en-us/windows/win32/shell/launch

            // TODO1 ? generic script runner - Use for all? ps, cmd, lua, py, ... or Default/builtin

            try
            {
                // Process the command string.
                if (args.Length != 3) { throw new($"invalid command format"); }
                var cmd = args[0];
                var tag = args[1];
                var path = args[2];

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
                        // WindowsMover: https://www.codeproject.com/Tips/1057230/Windows-Resize-and-Move
                        // C version: https://devblogs.microsoft.com/oldnewthing/20130610-00/?p=4133

                        pinfo.FileName = "explorer";
                        pinfo.Arguments = $"{dir}";
                        break;

                    case "newtab":
                        // Open a new explorer tab in current window at the dir selected in the first one. explorer middle button?
                        // Something like https://github.com/tariibaba/WinENFET/blob/main/src (autohotkey)./win-e.ahk
                        pinfo.FileName = "cmd";
                        pinfo.Arguments = $"/c echo !!newtab!! {DateTime.Now.Millisecond} | clip";
                        break;

                    case "tree": // direct => cmd /c tree /a /f "%V" | clip
                        // still flashes, ? Try ShellExecuteEx, setting nShow=SW_HIDE.
                        pinfo.FileName = "cmd";
                        pinfo.Arguments = $"/C tree \"{dir}\" /a /f | clip";
                        break;

                    case "openst": // direct
                        pinfo.FileName = "subl";
                        pinfo.Arguments = $"-n \"{dir}\"";
                        break;

                    case "find": // direct
                        pinfo.FileName = "everything";
                        pinfo.Arguments = $"-parent \"{dir}\"";
                        pinfo.WorkingDirectory = @"C:\Program Files\Everything";
                        break;

                    default:
                        throw new($"command verb: {cmd}");
                }

                var proc = new Process() { StartInfo = pinfo };
                proc.Start();
                //proc.WaitForExit();
                //if (proc.ExitCode != 0) { throw new($"process exit code: {proc.ExitCode}"); }

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
