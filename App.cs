using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Ephemera.NBagOfTricks;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Linq;


namespace Shellinator
{
    #region Types
    /// <summary>Internal exception.</summary>
    class ShellinatorException(string msg) : Exception(msg) { }

    /// <summary>
    /// Commands vary depending on which part of explorer they originate in. These are supported.
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
    /// <param name="Id">Internal id and registry key. System reserved: edit, explore, find, open, print, properties, runas.</param>
    /// <param name="Context">Where to install in `REG_ROOT`</param>
    /// <param name="Text">As it appears in the context menu.</param>
    /// <param name="Description">As it appears in the context menu.</param>
    /// <param name="Handler">Handle command.</param>
    readonly record struct ExplorerCommand(string Id, ExplorerContext Context, string Text, string Description, CommandHandler Handler);

    /// <summary>Command handler.</summary>
    /// <param name="context"></param>
    /// <param name="sel"></param>
    /// <param name="wdir"></param>
    delegate ExecResult CommandHandler(ExplorerContext context, string sel, string wdir);

    /// <summary>Convenience container.</summary>
    /// <param name="Code">Return code</param>
    /// <param name="Stdout">stdout if any</param>
    /// <param name="Stderr">stderr if any</param>
    readonly record struct ExecResult(int Code, string? Stdout, string? Stderr);
    #endregion

    /// <summary>Main app.</summary>
    public class App
    {
        #region Fields
        /// <summary>Simple profiling.</summary>
        readonly TimeIt _tmit = new();

        /// <summary>Where the exe lives.</summary>
        readonly string _exePath;

        /// <summary>Log file path name.</summary>
        readonly string _logPath;

        /// <summary>Dry run the registry writes.</summary>
        readonly bool _fake = true;

        /// <summary>All the app commands.</summary>
        readonly List<ExplorerCommand> _commands = [];
        #endregion

        #region The application
        /// <summary>Do the work.</summary>
        /// <param name="args"></param>
        public App(string[] args)
        {
            // Init stuff.
            _exePath = Environment.GetEnvironmentVariable("DEV_BIN_PATH");
            _logPath = Path.Join(_exePath, "shellinator.log");

            _commands =
            [
                new("treex",  ExplorerContext.Dir,   "Treex",              "Copy a tree of selected directory to clipboard", TreexCmd),
                new("treex",  ExplorerContext.DirBg, "Treex",              "Copy a tree here to clipboard.",                 TreexCmd),
                new("openst", ExplorerContext.Dir,   "Open in Sublime",    "Open selected directory in Sublime Text.",       SublimeCmd),
                new("openst", ExplorerContext.DirBg, "Open in Sublime",    "Open here in Sublime Text.",                     SublimeCmd),
                new("findev", ExplorerContext.Dir,   "Open in Everything", "Open selected directory in Everything.",         EverythingCmd),
                new("findev", ExplorerContext.DirBg, "Open in Everything", "Open here in Everything.",                       EverythingCmd),
                new("exec",   ExplorerContext.File,  "Execute",            "Execute file if executable otherwise open it.",  ExecCmd),
                // new("test", ExplorerContext.DeskBg, "!! Test DeskBg", "Debug stuff.", TestCmd),
                // new("test", ExplorerContext.Folder, "!! Test Folder", "Debug stuff.", TestCmd)
            ];

            // Test code.
            _commands.DistinctBy(p => p.Id).ForEach(c => Reg(c.Id));
            _commands.DistinctBy(p => p.Id).ForEach(c => Unreg(c.Id));

            // Process the args => Shellinator.exe id context sel wdir
            try
            {
                Log($"Shellinator command args:{string.Join(" ", args)}");

                _tmit.Snap("Here we go!");

                if (args.Length != 5)
                {
                    throw new ShellinatorException($"Invalid command line format");
                }

                var id = args[1];
                var context = (ExplorerContext)Enum.Parse(typeof(ExplorerContext), args[2]);
                var sel = Environment.ExpandEnvironmentVariables(args[3]);
                var wdir = Environment.ExpandEnvironmentVariables(args[4]);

                var cmdProc = _commands.FirstOrDefault(c => c.Id == id); // can throw if invalid

                // Run command.
                var res = cmdProc.Handler(context, sel, wdir);


                if (res.Code != 0)
                {
                    // Record any outputs.
                    Log($"Run() failed with [{res.Code}]");

                    if (res.Stdout is not null)
                    {
                        Log($"stdout: [{res.Stdout}]");
                    }
                    if (res.Stderr is not null)
                    {
                        Log($"stderr: [{res.Stderr}]");
                    }
                }
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
        #endregion

        #region All commands
        //--------------------------------------------------------//
        ExecResult TreexCmd(ExplorerContext context, string sel, string wdir)
        {
            var res = context switch
            {
                ExplorerContext.Dir => ExecuteCommand("cmd", sel, $"/c treex -c \"{wdir}\""),
                ExplorerContext.DirBg => ExecuteCommand("cmd", wdir, $"/c treex -c \"{wdir}\""),
                _ => throw new ShellinatorException($"Invalid context: {context}"),
            };

            return res;
        }

        //--------------------------------------------------------//
        ExecResult SublimeCmd(ExplorerContext context, string sel, string wdir)
        {
            var stpath = Path.Join(Environment.ExpandEnvironmentVariables("%ProgramFiles%"), "Sublime Text", "subl");

            var res = context switch
            {
                ExplorerContext.Dir => ExecuteCommand("cmd", sel, $"/c {stpath} --launch-or-new-window \"{wdir}\""),
                ExplorerContext.DirBg => ExecuteCommand("cmd", wdir, $"/c {stpath} --launch-or-new-window \"{wdir}\""),
                _ => throw new ShellinatorException($"Invalid context: {context}"),
            };

            return res;
        }

        //--------------------------------------------------------//
        ExecResult EverythingCmd(ExplorerContext context, string sel, string wdir)
        {
            var evpath = Path.Join(Environment.ExpandEnvironmentVariables("%ProgramFiles%"), "Everything", "everything");

            var res = context switch
            {
                ExplorerContext.Dir => ExecuteCommand("cmd", sel, $"/c {evpath} \"{wdir}\""),
                ExplorerContext.DirBg => ExecuteCommand("cmd", wdir, $"/c {evpath} \"{wdir}\""),
                _ => throw new ShellinatorException($"Invalid context: {context}"),
            };

            return res;
        }

        //--------------------------------------------------------//
        ExecResult ExecCmd(ExplorerContext context, string sel, string wdir)
        {
            var ext = Path.GetExtension(sel);

            var res = ext switch
            {
                ".cmd" or ".bat" => ExecuteCommand("cmd", wdir, $"/c \"{sel}\""),
                ".ps1" => ExecuteCommand("powershell", wdir, $"-executionpolicy bypass -File \"{sel}\""),
                ".lua" => ExecuteCommand("lua", wdir, $"\"{sel}\""),
                ".py" => ExecuteCommand("python", wdir, $"\"{sel}\""),
                _ => ExecuteCommand("cmd", wdir, $"/c \"{sel}\"") // default just open.
            };

            return res;
        }

        //--------------------------------------------------------//
        ExecResult TestCmd(ExplorerContext context, string sel, string wdir)
        {
            Notify($"!!! Got test [{context}] [{sel}] [{wdir}]", "Debug");
            return new();
        }
        #endregion

        #region Internals
        /// <summary>
        /// Generic command executor. Suppresses console window creation.
        /// </summary>
        /// <param name="exe"></param>
        /// <param name="wdir"></param>
        /// <param name="args"></param>
        /// <returns>Result code, stdout, stderr</returns>
        ExecResult ExecuteCommand(string exe, string wdir, string args)
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
            string? stdout = proc.StandardOutput.ReadToEnd();
            string? stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            return new(proc.ExitCode, stdout, stderr);
        }

        /// <summary>
        /// Write command to the registry. This generates registry entries that look like:
        /// [REG_ROOT\spec.RegPath\shell\spec.Id]
        /// @=""
        /// "MUIVerb"=spec.Text
        /// [REG_ROOT\spec.RegPath\shell\Id\command]
        /// @=spec.CommandLine
        /// </summary>
        /// <param name="id">Which command</param>
        void Reg(string id)
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

            //     Builtin macros:
            //     %L     : Selected file or directory name. Only Dir, File.
            //     %D     : Selected file or directory with expanded named folders. Only Dir, File, Folder.
            //     %V     : The directory of the selection, maybe unreliable? All except Folder.
            //     %W     : The working directory. All except Folder.
            //     %<0-9> : Positional arg.                                                
            //     %*     : Replace with all parameters.                                   
            //     %~     : Replace with all parameters starting with the second parameter.

            // All paths and macros that expand to paths must be wrapped in double quotes.
            // The builtin env vars like `%ProgramFiles%` are also supported.

            var cmds = _commands.Where(c => c.Id == id);
            if (!cmds.Any())
            {
                throw new ShellinatorException($"Invalid command: {id}");
            }

            foreach (var cmd in cmds)
            {
                using var hkcu = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
                using var regRoot = hkcu.OpenSubKey(@"Software\Classes", writable: true);

                // Key names etc.
                var ssubkey1 = $"{GetRegPath(cmd.Context)}\\shell\\{cmd.Id}";
                var ssubkey2 = $"{ssubkey1}\\command";
                // Assemble command: _exePath id context sel wdir
                var expCmd = $"\"{_exePath}\" {cmd.Id} {cmd.Context} %D %W";
                expCmd = Environment.ExpandEnvironmentVariables(expCmd);

                if (_fake)
                {
                    Log($"SetValue [{ssubkey1}] -> [MUIVerb={cmd.Text}]");
                    Log($"SetValue [{ssubkey2}] -> [@={expCmd}]");
                }
                else
                {
                    using var k1 = regRoot!.CreateSubKey(ssubkey1);
                    k1.SetValue("MUIVerb", cmd.Text);

                    using var k2 = regRoot!.CreateSubKey(ssubkey2);
                    k2.SetValue("", expCmd);
                }
            }
        }

        /// <summary>Delete existing registry entry.</summary>
        /// <param name="id">Which command</param>
        void Unreg(string id)
        {
            var cmds = _commands.Where(c => c.Id == id);
            if (!cmds.Any())
            {
                throw new ShellinatorException($"Invalid command: {id}");
            }

            foreach (var cmd in cmds)
            {
                using var hkcu = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
                using var regRoot = hkcu.OpenSubKey(@"Software\Classes", writable: true);

                // Key name.
                var ssubkey = $"{GetRegPath(cmd.Context)}\\shell\\{cmd.Id}";

                if (_fake)
                {
                    Log($"DeleteSubKeyTree [{ssubkey}]");
                }
                else
                {
                    regRoot!.DeleteSubKeyTree(ssubkey);
                }
            }
        }

        /// <summary>Convert the context enum to registry key.</summary>
        static string GetRegPath(ExplorerContext context)
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

        /// <summary>Simple logging.</summary>
        void Log(string msg, bool show = false)
        {
            File.AppendAllText(_logPath, $"{DateTime.Now:yyyy'-'MM'-'dd HH':'mm':'ss.fff} {msg}{Environment.NewLine}");
            if (show)
            {
                Notify(msg);
            }
        }

        /// <summary>Tell the user something.</summary>
        static void Notify(string msg, string caption = "")
        {
            MessageBox(IntPtr.Zero, msg, caption, 0);
        }

        /// <summary>Rudimentary UI notification for use in a console application.</summary>
        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        static extern int MessageBox(IntPtr hWnd, string msg, string caption, uint type);
        #endregion

        /// <summary>Where it all begins.</summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            new App(args);
        }
    }
}
