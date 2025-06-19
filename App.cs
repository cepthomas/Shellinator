using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Ephemera.NBagOfTricks;
using System.Linq;
using System.Drawing;
using System.ComponentModel;
using System.Text;
using System.Runtime.InteropServices;
using WI = Ephemera.Win32.Internals;
using WM = Ephemera.Win32.WindowManagement;
using CB = Ephemera.Win32.Clipboard;


?? Computer\HKEY_CURRENT_USER\Software\Classes\Directory\shellex\ContextMenuHandlers


namespace ShellEx
{
    class ShellExException(string msg, bool isError) : Exception(msg)
    {
        public bool IsError { get; } = isError;
    }


    /// <summary>See README#Commands. File to support specific extensions?</summary>
    public enum ExplorerContext { Dir, DirBg, DeskBg, Folder, File }


    public class ExplorerCommand
    {
        public string Id { get; init; } = "???";

        public ExplorerContext Context { get; init; } = ExplorerContext.Dir;

        public string Text { get; init; } = "???";

        public string CommandLine { get; init; } = "";

        public string Description { get; init; } = "";

        static List<string> _reservedIds = ["edit", "explore", "find", "open", "print", "properties", "runas"];

        public ExplorerCommand(string id, ExplorerContext context, string text, string cmdLine, string desc)
        {
            if (_reservedIds.Contains(id))
            {
                throw new ArgumentException($"Reserved id:{id}");
            }

            Id = id;
            Context = context;
            Text = text;
            CommandLine = cmdLine;
            Description = desc;
        }

        /// <summary>Readable version for property grid label.</summary>
        public override string ToString()
        {
            return $"{Id}: {Text}";
        }
    }

    public class App
    {
        #region Fields
        /// <summary>Measure performance. TODO1 use TimeIt</summary>
        readonly Stopwatch _sw = new();

        /// <summary>Result of command execution.</summary>
        string _stdout = "";

        /// <summary>Result of command execution.</summary>
        string _stderr = "";

        /// <summary>Log file name.</summary>
        readonly string _logFileName;

        /// <summary>Log debug stuff.</summary>
        bool _debug = false;

        /// <summary>Dry run the registry writes.</summary>
        readonly bool _fake = true;


        /// <summary>All the commands.</summary>
        List<ExplorerCommand> _commands =
        [
            new("cmder", ExplorerContext.Dir, "Commander", "%SHELLEX %ID \"%D\"", "Open a new explorer next to the current."),
            new("tree", ExplorerContext.Dir, "Tree", "%SHELLEX %ID \"%D\"", "Copy a tree of selected directory to clipboard"),
            new("openst", ExplorerContext.Dir, "Open in Sublime", "\"%ProgramFiles%\\Sublime Text\\subl\" --launch-or-new-window \"%D\"", "Open selected directory in Sublime Text."),
            new("findev", ExplorerContext.Dir, "Find in Everything", "%ProgramFiles%\\Everything\\everything -parent \"%D\"", "Open selected directory in Everything."),
            new("tree", ExplorerContext.DirBg, "Tree", "%SHELLEX %ID \"%W\"", "Copy a tree here to clipboard."),
            new("openst", ExplorerContext.DirBg, "Open in Sublime", "\"%ProgramFiles%\\Sublime Text\\subl\" --launch-or-new-window \"%W\"", "Open here in Sublime Text."),
            new("findev", ExplorerContext.DirBg, "Find in Everything", "%ProgramFiles%\\Everything\\everything -parent \"%W\"", "Open here in Everything."),
            new("exec", ExplorerContext.File, "Execute", "%SHELLEX %ID \"%D\"", "Execute file if executable otherwise opened."),
            new("test_deskbg", ExplorerContext.DeskBg, "!! Test DeskBg", "%SHELLEX %ID \"%W\"", "Debug stuff."),
            new("test_folder", ExplorerContext.Folder, "!! Test Folder", "%SHELLEX %ID \"%D\"", "Debug stuff."),
        ];

        #endregion

        /// <summary>Where it all begins.</summary>
        /// <param name="args"></param>
        public App(string[] args)
        {


            // Must do this first before initializing.
            string appDir = MiscUtils.GetAppDataDir("ShellEx", "Ephemera");

            _logFileName = Path.Join(appDir, "shellEx.txt");

            // Gets the icon associated with the currently executing assembly.
//            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);


            Log($"ShellEx command args:{string.Join(" ", args)}");

            // I'm in charge of the pixels.
            WI.DisableDpiScaling();

            Stopwatch sw = new();
            sw.Start();

            // Execute. Run throws various exceptions depending on the origin of the error.
            try
            {
                Run([.. args]);
                // OK here.
                Environment.ExitCode = 0;
                CB.SetText(_stdout);
            }
            catch (ShellExException ex)
            {
                if (ex.IsError)
                {
                    Environment.ExitCode = 1;
                    Log($"ShellEx ERROR: {ex.Message}");
                    CB.SetText($"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                    WI.MessageBox(ex.Message, "See the clipboard", true);
                }
                else // just notify
                {
                    Log($"ShellEx INFO: {ex.Message}");
                    Environment.ExitCode = 0;
                    WI.MessageBox(ex.Message, "You should know");
                }
            }
            catch (Win32Exception ex)
            {
                Log($"Spawned process ERROR: {ex.ErrorCode} {ex.Message}");
                CB.SetText($"{ex.Message}{Environment.NewLine}{_stderr}");
                Environment.ExitCode = 2;
                WI.MessageBox(ex.Message, "See the clipboard", true);
            }
            catch (Exception ex) // something else
            {
                Log($"Internal ERROR: {ex.Message}");
                CB.SetText($"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                Environment.ExitCode = 3;
                WI.MessageBox(ex.Message, "See the clipboard", true);
            }

            sw.Stop();
            Log($"Exit code:{Environment.ExitCode} msec:{sw.ElapsedMilliseconds}");

            // Before we end, manage log file.
            FileInfo fi = new(_logFileName);
            if (fi.Exists && fi.Length > 10000)
            {
                var lines = File.ReadAllLines(_logFileName);
                int start = lines.Length / 3;
                var trunc = lines.Subset(start, lines.Length - start);
                File.WriteAllLines(_logFileName, trunc);
                Log($"Trimmed log file", true);
            }
        }

        /// <summary>Do the work.</summary>
        /// <param name="args"></param>
        public void Run(List<string> args)
        {
            // Process the args => ShellEx.exe id path
            if (args.Count != 2)
            {
                throw new ShellExException($"Invalid command line format", true);
            }

            var id = Environment.ExpandEnvironmentVariables(args[0]);
            var path = Environment.ExpandEnvironmentVariables(args[1]);

            // Check for valid path.
            if (path.StartsWith("::"))
            {
                throw new ShellExException($"Can't use magic system folders e.g. Home", false);
            }
            else if (!Path.Exists(path))
            {
                throw new ShellExException($"Invalid path [{path}]", true);
            }

            // Final details.
            FileAttributes attr = File.GetAttributes(path);
            var wdir = attr.HasFlag(FileAttributes.Directory) ? path : Path.GetDirectoryName(path)!;
            var isdir = attr.HasFlag(FileAttributes.Directory);

            Log($"Run() id:{id} path:{path} wdir:{wdir} isdir:{isdir}", true);


            // Misc ui clickers.
//            btnDump.Click += (sender, e) => { WM.GetAppWindows("explorer").ForEach(w => tvInfo.AppendLine(w.ToString())); };

            // Manage commands in registry.
//            btnInitReg.Click += (sender, e) => { _commands.ForEach(c => c.CreateRegistryEntry(Path.Join(Environment.CurrentDirectory, "ShellEx.exe"))); };
//            btnClearReg.Click += (sender, e) => { _commands.ForEach(c => c.RemoveRegistryEntry()); };


            switch (id)
            {
                case "cmder":
                {
                    var fgHandle = WM.ForegroundWindow; // -> left pane
                    WM.AppWindowInfo fginfo = WM.GetAppWindowInfo(fgHandle);

                    // New explorer -> right pane.
                    WI.ShellExecute("explore", path);

                    // Locate the new explorer window. Wait for it to be created. This is a bit klunky but there does not appear to be a more direct method.
                    int tries = 0; // ~4
                    WM.AppWindowInfo? rightPane = null;
                    for (tries = 0; tries < 20 && rightPane is null; tries++)
                    {
                        System.Threading.Thread.Sleep(50);
                        var wins = WM.GetAppWindows("explorer");
                        rightPane = wins.Where(w => w.Title == path).FirstOrDefault();
                    }

                    if (rightPane is null)
                    {
                        throw new ShellExException($"Couldn't create right pane for [{path}]", true);
                    }

                    // Relocate/resize the windows to fit available real estate. TODO configurable? full screen?
                    WM.AppWindowInfo desktop = WM.GetAppWindowInfo(WM.ShellWindow);
                    Point loc = new(50, 50);
                    Size sz = new(desktop.DisplayRectangle.Width * 45 / 100, desktop.DisplayRectangle.Height * 80 / 100);

                    // Left pane.
                    WM.MoveWindow(fgHandle, loc);
                    WM.ResizeWindow(fgHandle, sz);
                    WM.ForegroundWindow = fgHandle;

                    // Right pane.
                    loc.Offset(sz.Width, 0);
                    WM.MoveWindow(rightPane.Handle, loc);
                    WM.ResizeWindow(rightPane.Handle, sz);
                    WM.ForegroundWindow = rightPane.Handle;
                }
                break;

                case "tree":
                {
                    int code = ExecuteCommand("cmd", wdir, $"/c tree /a /f \"{wdir}\"");
                    if (code != 0)
                    {
                        throw new Win32Exception(code);
                    }
                }
                break;

                case "exec":
                {
                    if (!isdir)
                    {
                        var ext = Path.GetExtension(path);
                        int code = ext switch
                        {
                            ".cmd" or ".bat" => ExecuteCommand("cmd", wdir, $"/c \"{path}\""),
                            ".ps1" => ExecuteCommand("powershell", wdir, $"-executionpolicy bypass -File \"{path}\""),
                            ".lua" => ExecuteCommand("lua", wdir, $"\"{path}\""),
                            ".py" => ExecuteCommand("python", wdir, $"\"{path}\""),
                            _ => ExecuteCommand("cmd", wdir, $"/c \"{path}\"") // default just open.
                        };
                        if (code != 0)
                        {
                            throw new Win32Exception(code);
                        }
                    }
                    // else ignore selection of dir
                }
                break;

                case "test_deskbg":
                case "test_folder":
                {
                    Log($"!!! Got {id}:{path}", true);
                    WI.MessageBox($"!!! Got {id}:{path}", "Debug");
                }
                break;

                default:
                    throw new ShellExException($"Invalid id:{id}", true);
            }
        }

        /// <summary>
        /// Generic command executor with hidden console.
        /// </summary>
        /// <param name="exe"></param>
        /// <param name="wdir"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        int ExecuteCommand(string exe, string wdir, string args)
        {
            Log($"ExecuteCommand() exe:{exe} wdir:{wdir} args:{args}", true);

            ProcessStartInfo pinfo = new()
            {
                FileName = exe,
                Arguments = args,
                WorkingDirectory = wdir, // needed?
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                //RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using Process proc = new() { StartInfo = pinfo };
            proc.Start();
            // TIL: To avoid deadlocks, always read the output stream first and then wait.
            _stdout = proc.StandardOutput.ReadToEnd();
            _stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            Log($"ExecuteCommand() exit:{proc.ExitCode}", true);

            return proc.ExitCode;
        }



        /// <summary>Write command to the registry.</summary>
        /// <param name="shellExPath"></param>
        void CreateRegistryEntry(string shellExPath)
        {
            using var hkcu = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
            using var regRoot = hkcu.OpenSubKey(@"Software\Classes", writable: true);

            // Key names etc.
            var ssubkey1 = $"{GetRegPath(Context)}\\shell\\{Id}";
            var ssubkey2 = $"{ssubkey1}\\command";
            var expCmd = CommandLine.Replace("%SHELLEX", $"\"{shellExPath}\"").Replace("%ID", Id);
            expCmd = Environment.ExpandEnvironmentVariables(expCmd);

            if (_fake)
            {
                Debug.WriteLine($"Create [{ssubkey1}]  MUIVerb={Text}");
                Debug.WriteLine($"Create [{ssubkey2}]  @={expCmd}");
            }
            else
            {
                using var k1 = regRoot!.CreateSubKey(ssubkey1);
                k1.SetValue("MUIVerb", Text);

                using var k2 = regRoot!.CreateSubKey(ssubkey2);
                k2.SetValue("", expCmd);
            }
        }

        /// <summary>Delete registry entry.</summary>
        void RemoveRegistryEntry()
        {
            using var hkcu = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
            using var regRoot = hkcu.OpenSubKey(@"Software\Classes", writable: true);

            // Key name.
            var ssubkey = $"{GetRegPath(Context)}\\shell\\{Id}";

            if (_fake)
            {
                Debug.WriteLine($"Delete [{ssubkey}]");
            }
            else
            {
                regRoot!.DeleteSubKeyTree(ssubkey);
            }
        }


        /// <summary>Convert the shellEx context to registry key.</summary>
        string GetRegPath(ExplorerContext context)
        {
            return context switch
            {
                ExplorerContext.Dir => "Directory",
                ExplorerContext.DirBg => "Directory\\Background",
                ExplorerContext.DeskBg => "DesktopBackground",
                ExplorerContext.Folder => "Folder",
                ExplorerContext.File => "*",
                _ => throw new ArgumentException("Impossible")
            };
        }


        // Create [Directory\shell\cmder]  MUIVerb=Commander
        // Create [Directory\shell\cmder\command]  @="C:\Dev\Apps\ShellEx\Ui\bin\x64\Debug\net8.0-windows\ShellEx.exe" cmder "%D"
        // Create [Directory\shell\tree]  MUIVerb=Tree
        // Create [Directory\shell\tree\command]  @="C:\Dev\Apps\ShellEx\Ui\bin\x64\Debug\net8.0-windows\ShellEx.exe" tree "%D"
        // Create [Directory\shell\openst]  MUIVerb=Open in Sublime
        // Create [Directory\shell\openst\command]  @="C:\Program Files\Sublime Text\subl" --launch-or-new-window "%D"
        // Create [Directory\shell\findev]  MUIVerb=Find in Everything
        // Create [Directory\shell\findev\command]  @=C:\Program Files\Everything\everything -parent "%D"
        // Create [Directory\Background\shell\tree]  MUIVerb=Tree
        // Create [Directory\Background\shell\tree\command]  @="C:\Dev\Apps\ShellEx\Ui\bin\x64\Debug\net8.0-windows\ShellEx.exe" tree "%W"
        // Create [Directory\Background\shell\openst]  MUIVerb=Open in Sublime
        // Create [Directory\Background\shell\openst\command]  @="C:\Program Files\Sublime Text\subl" --launch-or-new-window "%W"
        // Create [Directory\Background\shell\findev]  MUIVerb=Find in Everything
        // Create [Directory\Background\shell\findev\command]  @=C:\Program Files\Everything\everything -parent "%W"
        // Create [*\shell\exec]  MUIVerb=Execute
        // Create [*\shell\exec\command]  @="C:\Dev\Apps\ShellEx\Ui\bin\x64\Debug\net8.0-windows\ShellEx.exe" exec "%D"
        // Create [DesktopBackground\shell\test_deskbg]  MUIVerb=!! Test DeskBg
        // Create [DesktopBackground\shell\test_deskbg\command]  @="C:\Dev\Apps\ShellEx\Ui\bin\x64\Debug\net8.0-windows\ShellEx.exe" test_deskbg "%W"
        // Create [Folder\shell\test_folder]  MUIVerb=!! Test Folder
        // Create [Folder\shell\test_folder\command]  @="C:\Dev\Apps\ShellEx\Ui\bin\x64\Debug\net8.0-windows\ShellEx.exe" test_folder "%D"


        // Delete [Directory\shell\cmder]
        // Delete [Directory\shell\tree]
        // Delete [Directory\shell\openst]
        // Delete [Directory\shell\findev]
        // Delete [Directory\Background\shell\tree]
        // Delete [Directory\Background\shell\openst]
        // Delete [Directory\Background\shell\findev]
        // Delete [*\shell\exec]
        // Delete [DesktopBackground\shell\test_deskbg]
        // Delete [Folder\shell\test_folder]





        /// <summary>Simple logging, don't need or want a full-blown logger.</summary>
        void Log(string msg, bool debug = false)
        {
            if (_debug || !debug)
            {
                File.AppendAllText(_logFileName, $"{DateTime.Now:yyyy'-'MM'-'dd HH':'mm':'ss.fff} {(debug ? "DEBUG " : "")}{msg}{Environment.NewLine}");
            }
        }
    }
}
