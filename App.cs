using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Ephemera.NBagOfTricks;
using System.Runtime.InteropServices;


namespace Shellinator
{
    #region Types
    /// <summary>Internal exception.</summary>
    class ShellinatorException(string msg) : Exception(msg) { }

    /// <summary>
    /// Commands vary depending on which part of the explorer they originate in. These are supported.
    /// Operations on files are enabled generically, eventually specific extensions could be supported.
    /// </summary>
    [Flags]
    enum ExplorerContext
    {
        /// <summary>Right click in explorer right pane or windows desktop with a directory selected.</summary>
        Dir = 0x01,
        /// <summary>Right click in explorer right pane with nothing selected (background).</summary>
        DirBg = 0x02,
        /// <summary>Right click in windows desktop with nothing selected (background).</summary>
        DeskBg = 0x04,
        /// <summary>Right click in explorer left pane (navigation) with a folder selected.</summary>
        Folder = 0x08,
        /// <summary>Right click in explorer right pane or windows desktop with a file selected.</summary>
        File = 0x10
    }

    /// <summary>Describes one menu command.</summary>
    /// <param name="Id">Short name for internal id and registry key.</param>
    /// <param name="Context">Where to install in `REG_ROOT`</param>
    /// <param name="Text">As it appears in the context menu.</param>
    /// <param name="CommandLine">Full command string to execute. Supported macros:
    ///     Builtin macros:
    ///     %L     : Selected file or directory name. Only Dir, File.
    ///     %D     : Selected file or directory with expanded named folders. Only Dir, File, Folder.
    ///     %V     : The directory of the selection, maybe unreliable? All except Folder.
    ///     %W     : The working directory. All except Folder.
    ///     %<0-9> : Positional arg.                                                
    ///     %*     : Replace with all parameters.                                   
    ///     %~     : Replace with all parameters starting with the second parameter.
    /// 
    ///     Shellinator-specific macros:
    ///     %ID : The Id property value
    ///     %SHELLINATOR : Path to the Shellinator executable
    /// 
    ///     All paths and macros that expand to paths must be wrapped in double quotes.
    ///     The builtin env vars like `%ProgramFiles%` are also supported.
    /// </param>
    readonly record struct ExplorerCommand(string Id, ExplorerContext Context, string Text, string Description, string CommandLine);
    #endregion

    /// <summary>Main app.</summary>
    public class App
    {
        #region Fields
        /// <summary>Simple profiling.</summary>
        readonly TimeIt _tmit = new();

        /// <summary>Where the exe lives.</summary>
        string _shellinatorPath;

        /// <summary>Log file path name.</summary>
        readonly string _logPath;

        /// <summary>Dry run the registry writes.</summary>
        readonly bool _fake = true;

        /// <summary>All the builtin commands. Don't use reserved ids: edit, explore, find, open, print, properties, runas!!</summary>
        readonly List<ExplorerCommand> _commands =
        [
            new("treex",
                ExplorerContext.Dir,
                "Treex",
                "Copy a tree of selected directory to clipboard",
                "%SHELLINATOR %ID \"%D\""),

            new("tree",
                ExplorerContext.DirBg,
                "Tree",
                "Copy a tree here to clipboard.",
                "%SHELLINATOR %ID \"%W\""),

            new("openst",
                ExplorerContext.Dir,
                "Open in Sublime",
                "Open selected directory in Sublime Text.",
                "\"%ProgramFiles%\\Sublime Text\\subl\" --launch-or-new-window \"%D\""),

            new("openst",
                ExplorerContext.DirBg,
                "Open in Sublime",
                "Open here in Sublime Text.",
                "\"%ProgramFiles%\\Sublime Text\\subl\" --launch-or-new-window \"%W\""),

            new("findev",
                ExplorerContext.Dir,
                "Open in Everything",
                "Open selected directory in Everything.",
                "%ProgramFiles%\\Everything\\everything -parent \"%D\""),

            new("findev",
                ExplorerContext.DirBg,
                "Open in Everything",
                "Open here in Everything.",
                "%ProgramFiles%\\Everything\\everything -parent \"%W\""),

            new("exec",
                ExplorerContext.File,
                "Execute",
                "Execute file if executable otherwise opened.",
                "%SHELLINATOR %ID \"%D\""),

            //new("test_deskbg",
            //    ExplorerContext.DeskBg,
            //    "!! Test DeskBg",
            //    "Debug stuff."),
            //    "%SHELLINATOR %ID \"%W\"",

            //new("test_folder",
            //    ExplorerContext.Folder,
            //    "!! Test Folder",
            //    "Debug stuff."),
            //    "%SHELLINATOR %ID \"%D\"",
        ];
        #endregion

        /// <summary>Where it all begins.</summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            new App(args);
        }

        /// <summary>Do the work.</summary>
        /// <param name="args"></param>
        public App(string[] args)
        {
            //string appDir = MiscUtils.GetAppDataDir("Shellinator", "Ephemera");
            //_logFileName = Path.Join(appDir, "Shellinator.log");
            _logPath = Path.Join("\\", "Dev", "bin", "shellinator.log"); // TODO where?

            // This requires an env var named `DEV_BIN_PATH` - a known directory for the executable.
            _shellinatorPath = Environment.ExpandEnvironmentVariables("DEV_BIN_PATH");

            Log($"Shellinator command args:{string.Join(" ", args)}");

            _tmit.Snap("Here we go!");

            // Execute. Run throws various exceptions depending on the origin of the error.
            try
            {
                Environment.ExitCode = Run([.. args]);
                // Info already logged.
            }
            catch (ShellinatorException ex) // app error
            {
                Environment.ExitCode = 100;
                Log($"ShellinatorException: {ex.Message}", true);
            }
            catch (Exception ex) // something else
            {
                Environment.ExitCode = 101;
                Log($"Other Exception: {ex.Message}", true);
            }

            _tmit.Snap("All done");
            _tmit.Captures.ForEach(c => Log(c));

            // Before we end, manage log file.
            FileInfo fi = new(_logPath);
            if (fi.Exists && fi.Length > 10000)
            {
                var lines = File.ReadAllLines(_logPath);
                int start = lines.Length / 3;
                var trunc = lines.Subset(start, lines.Length - start);
                File.WriteAllLines(_logPath, trunc);
                Log($"============================ Trimmed log file ============================");
            }
        }

        /// <summary>Do the work.</summary>
        /// <param name="args"></param>
        /// <return>Execute return code.</return>
        public int Run(List<string> args)
        {
            // Process the args => Shellinator.exe id path
            if (args.Count != 2)
            {
                throw new ShellinatorException($"Invalid command line format");
            }

            var id = Environment.ExpandEnvironmentVariables(args[0]);
            var path = Environment.ExpandEnvironmentVariables(args[1]);

            // Check for valid path.
            if (path.StartsWith("::"))
            {
                throw new ShellinatorException($"Can't use magic system folders e.g. Home");
            }
            else if (!Path.Exists(path))
            {
                throw new ShellinatorException($"Invalid path [{path}]");
            }

            // Final details.
            FileAttributes attr = File.GetAttributes(path);
            var wdir = attr.HasFlag(FileAttributes.Directory) ? path : Path.GetDirectoryName(path)!;
            var isdir = attr.HasFlag(FileAttributes.Directory);

            Log($"Run() id:{id} path:{path} wdir:{wdir} isdir:{isdir}");

            (int code, string stdout, string stderr) ret = new();

            switch (id)
            {
                case "tree":
                {
                    ret = ExecuteCommand("cmd", wdir, $"/c tree /a /f \"{wdir}\"");
                }
                break;

                case "exec":
                {
                    if (!isdir)
                    {
                        var ext = Path.GetExtension(path);
                        ret = ext switch
                        {
                            ".cmd" or ".bat" => ExecuteCommand("cmd", wdir, $"/c \"{path}\""),
                            ".ps1" => ExecuteCommand("powershell", wdir, $"-executionpolicy bypass -File \"{path}\""),
                            ".lua" => ExecuteCommand("lua", wdir, $"\"{path}\""),
                            ".py" => ExecuteCommand("python", wdir, $"\"{path}\""),
                            _ => ExecuteCommand("cmd", wdir, $"/c \"{path}\"") // default just open.
                        };
                    }
                    // else ignore selection of dir
                }
                break;

                //case "_config":
                //{
                //    // Internal management commands.
                //    // write
                //    _commands.ForEach(CreateRegistryEntry);
                //    // delete
                //    _commands.ForEach(RemoveRegistryEntry);
                //}
                //break;

                //case "test_deskbg":
                //case "test_folder":
                //{
                //    Log($"!!! Got {id}:{path}");
                //    Notify($"!!! Got {id}:{path}", "Debug");
                //}
                //break;

                default:
                    throw new ShellinatorException($"Invalid id:{id}");
            }

            if (ret.code != 0)
            {
                // Record any outputs.
                Log($"Run failed with [{ret.code}]");
                if (ret.stdout != "")
                {
                    Log($"stdout: [{ret.stdout}]");
                }
                if (ret.stderr != "")
                {
                    Log($"stderr: [{ret.stderr}]");
                }
            }

            return ret.code;
        }

        /// <summary>
        /// Generic command executor. Suppresses console window creation.
        /// </summary>
        /// <param name="exe"></param>
        /// <param name="wdir"></param>
        /// <param name="args"></param>
        /// <returns>(code, stdout, stderr)</returns>
        (int code, string stdout, string stderr) ExecuteCommand(string exe, string wdir, string args)
        {
            Log($"ExecuteCommand() exe:[{exe}] wdir:[{wdir}] args:[{args}]");

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
            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            return (proc.ExitCode, stdout, stderr);
        }

        /// <summary>
        /// Write command to the registry. This generates registry entries that look like:
        /// [REG_ROOT\spec.RegPath\shell\spec.Id]
        /// @=""
        /// "MUIVerb"=spec.Text
        /// [REG_ROOT\spec.RegPath\shell\Id\command]
        /// @=spec.CommandLine
        /// </summary>
        /// <param name="ecmd">Which command</param>
        void CreateRegistryEntry(ExplorerCommand ecmd)
        {
            // Registry sections of interest:
            // - `HKEY_LOCAL_MACHINE` (HKLM): defaults for all users using a machine (administrator)
            // - `HKEY_CURRENT_USER` (HKCU): user specific settings (not administrator)
            // - `HKEY_CLASSES_ROOT` (HKCR): virtual hive of `HKEY_LOCAL_MACHINE` with `HKEY_CURRENT_USER` overrides (administrator)
            // `HKEY_CLASSES_ROOT` should be used only for reading currently effective settings. A write to `HKEY_CLASSES_ROOT` is
            // always redirected to `HKEY_LOCAL_MACHINE`\Software\Classes. 
            // In general, write directly to `HKEY_LOCAL_MACHINE\Software\Classes` or `HKEY_CURRENT_USER\Software\Classes` and read from `HKEY_CLASSES_ROOT`.
            // Shellinator bases all registry accesses (R/W) at `HKEY_CURRENT_USER\Software\Classes` aka `REG_ROOT`.
            // - General how to: https://learn.microsoft.com/en-us/windows/win32/shell/context-menu-handlers
            // - Detailed registry editing: https://mrlixm.github.io/blog/windows-explorer-context-menu/
            // - Shell command vars: https://superuser.com/questions/136838/which-special-variables-are-available-when-writing-a-shell-command-for-a-context

            using var hkcu = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
            using var regRoot = hkcu.OpenSubKey(@"Software\Classes", writable: true);

            // Key names etc.
            var ssubkey1 = $"{GetRegPath(ecmd.Context)}\\shell\\{ecmd.Id}";
            var ssubkey2 = $"{ssubkey1}\\command";
            var expCmd = ecmd.CommandLine.Replace("%SHELLINATOR", $"\"{_shellinatorPath}\"").Replace("%ID", ecmd.Id);
            expCmd = Environment.ExpandEnvironmentVariables(expCmd);

            if (_fake)
            {
                Debug.WriteLine($"SetValue [{ssubkey1}] -> [MUIVerb={ecmd.Text}]");
                Debug.WriteLine($"SetValue [{ssubkey2}] -> [@={expCmd}]");
            }
            else
            {
                using var k1 = regRoot!.CreateSubKey(ssubkey1);
                k1.SetValue("MUIVerb", ecmd.Text);

                using var k2 = regRoot!.CreateSubKey(ssubkey2);
                k2.SetValue("", expCmd);
            }
        }

        /// <summary>Delete existing registry entry.</summary>
        void RemoveRegistryEntry(ExplorerCommand ecmd)
        {
            using var hkcu = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
            using var regRoot = hkcu.OpenSubKey(@"Software\Classes", writable: true);

            // Key name.
            var ssubkey = $"{GetRegPath(ecmd.Context)}\\shell\\{ecmd.Id}";

            if (_fake)
            {
                Debug.WriteLine($"DeleteSubKeyTree [{ssubkey}]");
            }
            else
            {
                regRoot!.DeleteSubKeyTree(ssubkey);
            }
        }

        /// <summary>Convert the shell context to registry key.</summary>
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

        /// <summary>Simple logging, don't need a full-blown logger.</summary>
        void Log(string msg, bool show = false)
        {
            File.AppendAllText(_logPath, $"{DateTime.Now:yyyy'-'MM'-'dd HH':'mm':'ss.fff}{msg}{Environment.NewLine}");
            if (show)
            {
                Notify(msg);
            }
        }

        /// <summary>Tell the user.</summary>
        void Notify(string msg, string caption = "")
        {
            MessageBox(IntPtr.Zero, msg, caption, 0);
        }

        /// <summary>Rudimentary UI notification for use in a console application.</summary>
        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        static extern int MessageBox(IntPtr hWnd, string msg, string caption, uint type);
    }
}
