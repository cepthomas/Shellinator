using System;
using System.IO;
using System.Collections.Generic;
using Ephemera.NBagOfTricks;
using System.Diagnostics;


namespace Splunk
{


// TODO1 still gotta figure out the cmd <> without terminal. See what python does.
// case "tree": // direct => cmd /c tree /a /f "%V" | clip
//     // still flashes, ? Try ShellExecuteEx, setting nShow=SW_HIDE.


    //C:\Lua\lua.exe "C:\Dev\repos\Apps\Splunk\test.lua"

    //"C:\Dev\repos\Apps\Splunk\Splunk2\bin\Debug\net8.0\Splunk.exe"


    // IPC client
    // try
    // {
    //     // Clean up args and make them safe for server by quoting before concatenating.
    //     List<string> cleanArgs = [];
        
    //     // Fix corner case for accidental escaped quote.
    //     args.ForEach(a => { cleanArgs.Add($"\"{a.Replace("\"", "")}\""); });
    //     var cmdString = string.Join(" ", cleanArgs);
    //     //twr.WriteLine($"Send: {cmdString}");

    //     Ephemera.NBagOfTricks.SimpleIpc.Client ipcClient = new(Common.Common.PIPE_NAME, Common.Common.LogFileName);
    //     var res = ipcClient.Send(cmdString, 1000);
    //     if (res != Ephemera.NBagOfTricks.SimpleIpc.ClientStatus.Ok)
    //     {
    //         twr.WriteLine($"Result: {res} {ipcClient.Error}");
    //     }
    // }
    // catch (Exception ex)
    // {
    //     twr.WriteLine($"Client failed: {ex.Message}");
    // }





    /// <summary>The worker.</summary>
    public class Splunk
    {
        public int Run(string[] args, TextWriter twr)
        {
            int ret = 0;

            //tvInfo.AppendLine($"ProcessMessage() {Environment.CurrentManagedThreadId}");

            try
            {
                //                string dir;
                // Current bin dir. C:\Dev\repos\Apps\Splunk\Splunk\bin\Debug\net8.0-windows
                // ==== CreateRegistryEntries(Environment.CurrentDirectory);



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
                        // TODO option for full screen?

                        pinfo.FileName = "explorer";
                        pinfo.Arguments = $"{dir}";
                        break;

                    case "newtab":
                        // Open a new explorer tab in current window at the dir selected in the first one.
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


                //_proc.StartInfo = pinfo;
                //_proc.Start();
                //_proc.WaitForExit();
                //_proc.Close();

                var proc = new Process() { StartInfo = pinfo };
                proc.Start();
                //proc.WaitForExit();
                //if (proc.ExitCode != 0) { throw new($"process exit code: {proc.ExitCode}"); }

            }
            catch (Exception ex) // handle errors
            {
                twr.WriteLine("ERROR " + ex.Message);

                //tvInfo.AppendLine("ERROR " + ex.Message);
                //_log.Write("ERROR " + ex.Message);
                ret = 1;
            }

            return ret;
        }
    }
}
