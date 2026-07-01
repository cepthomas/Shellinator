using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Linq;
using System.Collections;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using Ephemera.NBagOfTricks;


namespace Shellinator
{
    /// <summary>The generic part of the app.</summary>
    internal partial class App
    {
        #region Fields
        /// <summary>Simple profiling.</summary>
        readonly TimeIt _tmit = new();

        /// <summary>Where the exe lives.</summary>
        readonly string _exePath;

        /// <summary>Log file path name.</summary>
        readonly string _logPath;

        /// <summary>Dry run the registry writes.</summary>
        readonly bool _fake = true; //false;

        /// <summary>Don't use reserved commands.</summary>
        readonly List<string> _reserved = ["edit", "explore", "find", "open", "print", "properties", "runas"];

        /// <summary>All the app commands.</summary>
        List<ExplorerCommand> _commands = [];

        /// <summary>New flavor - All the app commands.</summary>
        readonly List<ExplorerCommandNuevo> _commandsNuevo = [];

        /// <summary>Determine mode.</summary>
        bool _inDev = false;
        #endregion

        /// <summary>Where it all begins.</summary>
        /// <param name="args"></param>
        [STAThread]
        static void Main(string[] args)
        {
            new App(args);
        }

        #region The command line application
        /// <summary>Do the work.</summary>
        /// <param name="args"></param>
        public App(string[] args)
        {
            int code = 0;

            try
            {
                _tmit.Snap("App constructor");

                ///// Init internal stuff.
                _inDev = Debugger.IsAttached; // or look through Process.GetCurrentProcess().Modules

                // Standard path supplied?
                var toolsPath = Environment.GetEnvironmentVariable("TOOLS_PATH") ?? throw new ShellinatorException("Environment missing TOOLS_PATH");
                _exePath = toolsPath;
                if (!Path.Exists(_exePath)) { throw new ShellinatorException($"Missing folder {_exePath}"); }

                // Init log.
                _logPath = Path.Join(_exePath, "shellinator.log");
                FileInfo fi = new(_logPath);
                if (fi.Exists && fi.Length > 10000)
                {
                    var newfn = _logPath.Replace(".log", "_old.log");
                    File.Delete(newfn);
                    File.Move(_logPath, newfn);
                }

                // Set up commands.
                // old style
                InitCommands();

                // Nuevo style. Config file is assumed to be next to the executable. TODO1 init a default
                _tmit.Snap("Start load config");
                var dir = Path.GetDirectoryName(Application.ExecutablePath);
                var cfn = Path.Combine(dir, _inDev ? "default.ini" : "shellinator.ini");
                LoadIni(cfn);
                _tmit.Snap("Done load config");


                ///// Process the args: shellinator.exe id context target.
                string id = args.Length > 0 ? args[0].ToLower() : "No args!";
                switch (args.Length, id)
                {
                    case (1, "reg"):
                        RegisterAll();
                        break;

                    case (1, "unreg"):
                        UnregisterAll();
                        break;

                    case (1, "dev"):
                        LogInfo($"Shellinator dev mode");

                        ///// New dev code. /////
                        //LoadIni(@"C:\Dev\Apps\Shellinator\commands.ini");

                        //var dump = DumpHive(RegistryHive.CurrentUser, @"Software\Classes");
                        //// DumpHive(RegistryHive.LocalMachine, @"Software\Classes");
                        //// DumpHive(RegistryHive.CurrentUser, @"\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts");
                        //dump.ForEach(x => Console.WriteLine(x));

                        ///// Original dev code. /////
                        //_fake = true;
                        //LogInfo($"Shellinator register all");
                        //_commands.DistinctBy(p => p.Id).ForEach(c => Reg(c.Id));
                        //LogInfo($"Shellinator unregister all");
                        //_commands.DistinctBy(p => p.Id).ForEach(c => Unreg(c.Id));
                        //var tcmd = TestCmd(ExplorerContext.Dir, "Do stuff...");
                        break;

                    case (3, _):
                        // Normal mode called from system using registry entry.
                        _tmit.Snap("Execute command:");
                        LogInfo($"Shellinator command args:{string.Join(" ", args)}");

                        var context = (ExplorerContext)Enum.Parse(typeof(ExplorerContext), args[1]);
                        var target = Environment.ExpandEnvironmentVariables(args[2]);
                        var cmdProc = _commands.FirstOrDefault(c => c.Id == id);
                        // Run command.
                        var res = cmdProc.Handler(context, target);

                        if (res.Code == 0)
                        {
                            // Success. Capture any stdout. TIL don't set clipboard to an empty string.
                            Clipboard.SetText(res.Stdout.Length > 0 ? res.Stdout : "Success");
                        }
                        else
                        {
                            // Command failed. Capture everything useful.
                            List<string> ls = [];
                            ls.Add($">>> FAILED!!!{Environment.NewLine}code: {res.Code}");
                            ls.Add($">>> stdout:{Environment.NewLine}{(res.Stdout.Length > 0 ? res.Stdout : "None")}");
                            ls.Add($">>> stderr:{Environment.NewLine}{(res.Stderr.Length > 0 ? res.Stderr : "None")}");
                            var sres = string.Join(Environment.NewLine, ls);
                            LogError(sres);
                            Clipboard.SetText(sres);
                            code = 1;
                        }
                        break;

                    default:
                        throw new ShellinatorException($"Invalid command line args: [{string.Join(" ", args)}]");
                }
            }
            catch (ShellinatorException ex) // app error
            {
                LogError($"{ex}");
                Clipboard.SetText(ex.ToString());
                MessageBox.Show(ex.ToString());
                code = 2;
            }
            catch (Exception ex) // something else
            {
                LogError($"{ex}");
                Clipboard.SetText(ex.ToString());
                MessageBox.Show(ex.ToString());
                code = 3;
            }

            _tmit.Snap("All done");
            _tmit.Captures.ForEach(c => LogInfo($"TMIT [{c}]"));

            Environment.Exit(code);
        }
        #endregion



        /// <summary>
        /// 
        /// </summary>
        void RegisterAllNuevo()
        {
            LogInfo($"Shellinator register all"); // TODO1 keep a list of installed keys so unreg is cleaner.
            UnregisterAllNuevo(); // clean up first

            _commandsNuevo.ForEach(cmd => { RegisterNuevo(cmd); });

           // _commands.DistinctBy(p => p.Id).ForEach(c => Register(c.Id));
        }

        /// <summary>
        /// 
        /// </summary>
        void UnregisterAllNuevo()
        {
            LogInfo($"Shellinator unregister all");

            _commandsNuevo.ForEach(cmd => { UnregisterNuevo(cmd); });

            //_commands.DistinctBy(p => p.Id).ForEach(c => Unregister(c.Id));
        }




        #region Internals
        /// <summary>
        /// 
        /// </summary>
        void RegisterAll()
        {
            LogInfo($"Shellinator register all"); // TODO1 keep a list of installed keys so unreg is cleaner.
            UnregisterAll(); // clean up first

            _commands.DistinctBy(p => p.Id).ForEach(c => Register(c.Id));
        }

        /// <summary>
        /// 
        /// </summary>
        void UnregisterAll()
        {
            LogInfo($"Shellinator unregister all");

            _commands.DistinctBy(p => p.Id).ForEach(c => Unregister(c.Id));
        }

        /// <summary>
        /// Generic command executor. Called by command handlers. Suppresses console window creation.
        /// </summary>
        /// <param name="args">All args including command first.</param>
        /// <param name="cmd">True for command line commands. Wraps the call in 'cmd /C'.</param>
        /// <returns>Result code, stdout, stderr</returns>
        ExecResult ExecuteCommand(List<string> args, bool cmd = false)
        {
            //LogInfo($"ExecuteCommand():[{string.Join(" ", args)}] cmd:{cmd}");

            if (cmd)
            {
                args.InsertRange(0, ["cmd", "/C"]);
            }

            ProcessStartInfo pinfo = new(args[0], args[1..])
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            // Add app-specific environmental variables.
            // pinfo.EnvironmentVariables["MY_VAR"] = "Hello!";

            using Process proc = new() { StartInfo = pinfo };
            //proc.Exited += (sender, e) => { LogInfo("Process exit event."); };

            //LogInfo("Start process...");
            proc.Start();

            // TIL: To avoid deadlocks, always read the output stream first and then wait.
            var stdout = proc.StandardOutput.ReadToEnd();
            var stderr = proc.StandardError.ReadToEnd();

            // LogInfo("Wait for process to exit...");
            proc.WaitForExit();
            //LogInfo("Exited.");

            return new(proc.ExitCode, stdout, stderr);
        }

        /// <summary>
        /// Load an ini config file.
        /// </summary>
        /// <param name="fn"></param>
        /// <exception cref="IniSyntaxException"></exception>
        /// <exception cref="ArgumentException"></exception>
        void LoadIni(string fn)
        {
            // Init runtime values from ini file.
            var inrdr = new IniReader();
            inrdr.ParseFile(fn);

            foreach (var sectName in inrdr.GetSectionNames())
            {
                var sectionParts = sectName.SplitByTokens(" ");
                if (sectionParts.Count < 2) throw new IniSyntaxException($"Invalid section name [{sectName}]", -1);

                var ctxt = sectionParts[0];
                var id = sectionParts[1];
                var sectVals = inrdr.GetValues(sectName);

                if (!sectVals.TryGetValue("menu", out var menu)) throw new IniSyntaxException($"Missing menu item in section [{sectName}]", -1);
                if (!sectVals.TryGetValue("command", out var command)) throw new IniSyntaxException($"Missing command item in section [{sectName}]", -1);
                if (_reserved.Contains(id)) { throw new ArgumentException($"Reserved key [{id}]"); }
                switch (ctxt.ToLower())
                {
                    case "dir":
                        _commandsNuevo.Add(new(ExplorerContext.Dir, id, menu, command));
                        break;

                    case "dirbg":
                        _commandsNuevo.Add(new(ExplorerContext.DirBg, id, menu, command));
                        break;

                    case "deskbg":
                        _commandsNuevo.Add(new(ExplorerContext.DeskBg, id, menu, command));
                        break;

                    case "file":
                        sectionParts[1..].ForEach(p => _commandsNuevo.Add(new(ExplorerContext.File, p, menu, command)));
                        break;

                    //case "folder":
                    //    break;

                    default:
                        throw new ArgumentException($"Invalid context: {sectionParts[0]}");
                }
            }
        }
        #endregion




        /// <summary>
        /// Write a command to the registry.
        /// </summary>
        /// <param name="cmd">Which command</param>
        void RegisterNuevo(ExplorerCommandNuevo cmd)//  string id)
        {
            // This generates registry entries that look like:
            // [REG_ROOT\command.RegPath\shell\command.Id]
            // @=""
            // "MUIVerb"=command.Text
            // [REG_ROOT\command.RegPath\shell\Id\command]
            // @=command.CommandLine

            //var cmds = _commands.Where(c => c.Id == id);
            //if (!cmds.Any()) { throw new ShellinatorException($"Invalid command: {id}"); }
            //if (_reserved.Contains(id)) { throw new ArgumentException($"Invalid command: {id}"); }

            //foreach (var cmd in cmds)
            {
                // Assemble command: _exePath id context target

                using var hkcu = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
                using var regRoot = hkcu.OpenSubKey(@"Software\Classes", writable: true);

                // Key names etc.
                var ssubkey1 = $"{GetRegPath(cmd.Context)}\\shell\\{cmd.Id}";
                var ssubkey2 = $"{ssubkey1}\\command";

                // Determine target based on origin. Dir/File/Folder:%D DirBg/DeskBg:%W.
                var target = cmd.Context switch
                {
                    ExplorerContext.DirBg => "%W",
                    ExplorerContext.DeskBg => "%W",
                    _ => "%D",
                };

                // All paths and macros that expand to paths must be wrapped in double quotes.
                // The builtin env vars like %ProgramFiles% are also supported.
                var exec = Path.Join(_exePath, "shellinator.exe");
                var expCmd = $"\"{exec}\" {cmd.Id} {cmd.Context} \"{target}\"";
                expCmd = Environment.ExpandEnvironmentVariables(expCmd);

                if (_fake)
                {
                    LogInfo($"SetValue [{ssubkey1}] -> [MUIVerb={cmd.Text}]");
                    LogInfo($"SetValue [{ssubkey2}] -> [@={expCmd}]");
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
        /// <param name="cmd">Which command</param>
        void UnregisterNuevo(ExplorerCommandNuevo cmd)// string id)
        {
            //var cmds = _commands.Where(c => c.Id == id);
            //if (!cmds.Any())
            //{
            //    throw new ShellinatorException($"Invalid command: {id}");
            //}

           // foreach (var cmd in cmds)
            {
                using var hkcu = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
                using var regRoot = hkcu.OpenSubKey(@"Software\Classes", writable: true);

                // Key name.
                var ssubkey = $"{GetRegPath(cmd.Context)}\\shell\\{cmd.Id}";

                if (_fake)
                {
                    LogInfo($"DeleteSubKeyTree [{ssubkey}]");
                }
                else
                {
                    regRoot!.DeleteSubKeyTree(ssubkey, false);
                }
            }
        }










        #region Registry editing
        /// <summary>
        /// Write a command to the registry.
        /// </summary>
        /// <param name="id">Which command</param>
        void Register(string id)
        {
            // This generates registry entries that look like:
            // [REG_ROOT\command.RegPath\shell\command.Id]
            // @=""
            // "MUIVerb"=command.Text
            // [REG_ROOT\command.RegPath\shell\Id\command]
            // @=command.CommandLine

            var cmds = _commands.Where(c => c.Id == id);
            if (!cmds.Any()) { throw new ShellinatorException($"Invalid command: {id}"); }
            if (_reserved.Contains(id)) { throw new ArgumentException($"Invalid command: {id}"); }

            foreach (var cmd in cmds)
            {
                // Assemble command: _exePath id context target

                using var hkcu = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64);
                using var regRoot = hkcu.OpenSubKey(@"Software\Classes", writable: true);

                // Key names etc.
                var ssubkey1 = $"{GetRegPath(cmd.Context)}\\shell\\{cmd.Id}";
                var ssubkey2 = $"{ssubkey1}\\command";

                // Determine target based on origin. Dir/File/Folder:%D DirBg/DeskBg:%W.
                var target = cmd.Context switch
                {
                    ExplorerContext.DirBg => "%W",
                    ExplorerContext.DeskBg => "%W",
                    _ => "%D",
                };

                // All paths and macros that expand to paths must be wrapped in double quotes.
                // The builtin env vars like %ProgramFiles% are also supported.
                var exec = Path.Join(_exePath, "shellinator.exe");
                var expCmd = $"\"{exec}\" {cmd.Id} {cmd.Context} \"{target}\"";
                expCmd = Environment.ExpandEnvironmentVariables(expCmd);

                if (_fake)
                {
                    LogInfo($"SetValue [{ssubkey1}] -> [MUIVerb={cmd.Text}]");
                    LogInfo($"SetValue [{ssubkey2}] -> [@={expCmd}]");
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
        void Unregister(string id)
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
                    LogInfo($"DeleteSubKeyTree [{ssubkey}]");
                }
                else
                {
                    regRoot!.DeleteSubKeyTree(ssubkey, false);
                }
            }
        }

        /// <summary>Convert the internal enum to registry key.</summary>
        static string GetRegPath(ExplorerContext context)
        {
            return context switch
            {
                ExplorerContext.Dir => "Directory",
                ExplorerContext.DirBg => "Directory\\Background",
                ExplorerContext.DeskBg => "DesktopBackground",
                ExplorerContext.File => "*",
                _ => throw new ArgumentException("Impossible")
            };
        }
        #endregion

        #region Common infrastructure
        /// <summary>Simple logging and notification.</summary>
        void LogInfo(string msg, [CallerFilePath] string file = "", [CallerLineNumber] int line = -1)
        {
            // Always log.
            var fspec = file != "" ? $"{Path.GetFileName(file)}({line}) " : " ";
            File.AppendAllText(_logPath, $"{DateTime.Now:yyyy'-'MM'-'dd HH':'mm':'ss.fff} {fspec}{msg}{Environment.NewLine}");
        }

        /// <summary>Simple logging and notification.</summary>
        void LogError(string msg, [CallerFilePath] string file = "", [CallerLineNumber] int line = -1)
        {
            // Always log.
            var fspec = file != "" ? $"{Path.GetFileName(file)}({line}) " : " ";
            File.AppendAllText(_logPath, $"{DateTime.Now:yyyy'-'MM'-'dd HH':'mm':'ss.fff} ERROR {fspec}{msg}{Environment.NewLine}");
            //MessageBox.Show($"{fspec}{msg}", "Command failed");
        }

        /// <summary>
        /// Utility to look at registry contents of interest.
        /// </summary>
        /// <param name="hive"></param>
        /// <param name="subkey"></param>
        /// <param name="recursive"></param>
        List<string> DumpHive(RegistryHive hive, string subkey, bool recursive = true)
        {
            List<string> res =
            [
                $"",
                $"====================== {hive} {subkey} ======================",
                $""
            ];

            using var hkcr = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var regRoot = hkcr.OpenSubKey(subkey, writable: false);

            List<string> contexts = ["Directory", "DesktopBackground", "Folder", "*"];
            contexts.ForEach(ctx => DoSubkey(regRoot!, ctx));

            void DoSubkey(RegistryKey key, string sname, int indent = 0)
            {
                string sind = new(' ', indent * 4);
                res.Add($"{sind}[{sname}]");

                using var subkey = key.OpenSubKey(sname, writable: false);
                if (subkey is null) return;

                foreach (string sval in subkey.GetValueNames())
                {
                    // "" means default
                    res.Add($"{sind}  [{sval}]:[{subkey.GetValue(sval)}]");
                }

                if (recursive)
                {
                    // Visit the children.
                    var snames = subkey.GetSubKeyNames();
                    snames.ForEach(s => DoSubkey(subkey, s, indent + 1));
                }
            }

            return res;
        }
        #endregion
    }
}
