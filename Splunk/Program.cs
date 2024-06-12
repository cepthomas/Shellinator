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

// TODO1 plugin model?
test from UI

namespace Splunk
{
    public class Program
    {
        /// <summary>Crude debugging without spinning up a console or logger.</summary>
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
            Environment.ExitCode = Run([.. args]);
            sw.Stop();
            _debug.Add($"{DateTime.Now} Splunk [{string.Join(" ", args)}] Exit:{Environment.ExitCode} Msec:{sw.ElapsedMilliseconds}");
            File.AppendAllLines(Path.Join(appDir, "debug.txt"), _debug);
        }

        /// <summary>Do the work.</summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static int Run(List<string> args)
        {
            int ret = 0;

            try
            {
                // Process the args.
                if (args.Count != 2) { throw new($"Invalid command format"); }
                var cmd = Environment.ExpandEnvironmentVariables(args[0]);
                var path = args[1];
                // Check for valid path.
                if (!Path.Exists(path)) { throw new($"Invalid path: {path}"); }
                FileAttributes attr = File.GetAttributes(path);
                var dir = attr.HasFlag(FileAttributes.Directory) ? path : Path.GetDirectoryName(path)!;
                var isdir = attr.HasFlag(FileAttributes.Directory);

                switch (cmd)
                {
                    case "cmder":
                        var fgHandle = NM.GetForegroundWindow(); // -> left pane
                        WindowInfo fginfo = SU.GetWindowInfo(fgHandle);
                        // New explorer -> right pane.
                        NM.ShellExecute(IntPtr.Zero, "explore", path, IntPtr.Zero, IntPtr.Zero, (int)NM.ShowCommands.SW_NORMAL);
                        // Locate the new explorer window. Wait for it to be created.
                        // This is a bit klunky but there does not appear to be a more direct method.
                        int tries = 0;
                        WindowInfo? rightPane = null;
                        for (tries = 0; tries < 20 && rightPane is null; tries++) // ~4
                        {
                            System.Threading.Thread.Sleep(50);
                            var wins = SU.GetAppWindows("explorer");
                            rightPane = wins.Where(w => w.Title == path).FirstOrDefault();
                        }
                        if (rightPane is null) throw new InvalidOperationException($"Couldn't create right pane for [{path}]");

                        // Relocate the windows. TODO Get size from settings?.
                        WindowInfo desktop = SU.GetWindowInfo(NM.GetShellWindow());
                        int w = desktop.DisplayRectangle.Width * 45 / 100;
                        int h = desktop.DisplayRectangle.Height * 80 / 100;
                        int t = 50, l = 50;
                        _ = NM.MoveWindow(fgHandle, l, t, w, h, true);
                        NM.SetForegroundWindow(fgHandle);
                        _ = NM.MoveWindow(rightPane.Handle, l + w, t, w, h, true);
                        NM.SetForegroundWindow(rightPane.Handle);
                        break;

                    case "tree":
                        using (Process proc = new() { StartInfo = MakeStartInfo() })
                        {
                            proc.Start();
                            proc.StandardInput.WriteLine($"tree /a /f \"{dir}\" | clip");
                            proc.StandardInput.WriteLine($"exit");
                            proc.WaitForExit();
                            if (proc.ExitCode != 0) throw new($"Process failed: {proc.ExitCode}");
                        }
                        break;

                    case "exec":
                        if (!isdir)
                        {
                            var ext = Path.GetExtension(path);
                            switch (Path.GetExtension(path))
                            {
                                case ".cmd":
                                case ".bat":


                                    break;

                                case ".lua":
                                    //    cmd_list.append('lua')
                                    //    cmd_list.append(f'\"{path}\"')

                                    break;

                                case ".py":
                                    //    cmd_list.append('python')
                                    //    cmd_list.append(f'\"{path}\"')

                                    break;

                                default:
                                    // just open

                                    break;



                            }

                        }
                        else
                        {
                            

                        }

                            //cp = subprocess.run(cmd, cwd=dir, universal_newlines=True, capture_output=True, shell=True)  # check=True)
                            //output = cp.stdout
                            //errors = cp.stderr
                            //if len(errors) > 0:
                            //    output = output + '============ stderr =============\n' + errors
                            //sc.create_new_view(self.window, output)





                        break;


                    case "test_deskbg":
                        NM.MessageBox(IntPtr.Zero, "ExplorerContext.DeskBg", "My Message Box", 0);
                        break;

                    case "test_folder":
                        NM.MessageBox(IntPtr.Zero, "ExplorerContext.Folder", "My Message Box", 0);
                        break;

                    default:
                        throw new ArgumentException($"Invalid command: {cmd}");
                }

                // Generic command window, hidden.
                ProcessStartInfo MakeStartInfo()
                {
                    return new()
                    {
                        FileName = "cmd",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = dir,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                    };
                }
            }
            catch (Exception ex)
            {
                _debug.Add($"ERROR:{DateTime.Now} {ex.Message}");
                NM.MessageBox(IntPtr.Zero, "Your Message", "My Message Box", 0);
                ret = 1;
            }

            return ret;
        }
    }
}
