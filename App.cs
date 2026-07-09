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
    class App
    {
        #region Types
        /// <summary>Internal exception.</summary>
        class ShellinatorException(string msg, [CallerLineNumber] int line = -1) : Exception(msg)
        {
            public int Line { get; } = line;
            public override string ToString() { return $"({Line}) {base.ToString()})"; }
        }

        /// <summary>Describes one standard menu command.</summary>
        /// <param name="Context">Where to install.</param>
        /// <param name="Key">Registry key/command. Can't use reserved: edit, explore, find, open, print, properties, runas.</param>
        /// <param name="Text">As it appears in the context menu.</param>
        /// <param name="ExecLine">Command line args to execute.</param>
        record ExplorerCommand(string Context, string Key, string Text, List<string> ExecLine)
        {
            public override string ToString() { return $"ExplorerCommand Context:[{Context}] Key:[{Key}] Text:[{Text}] ExecLine:[{string.Join("|", ExecLine)}]"; }
        };

        /// <summary>Describes one run menu command.</summary>
        /// <param name="Ext">File extension.</param>
        /// <param name="ExecLine">Command line args to execute.</param>
        record RunCommand(string Ext, List<string> ExecLine)
        {
            public override string ToString() { return $"RunCommand Ext:[{Ext}] ExecLine:[{string.Join("|", ExecLine)}]"; }
        };

        /// <summary>Convenience container.</summary>
        /// <param name="Code">Return code</param>
        /// <param name="Stdout">stdout if any</param>
        /// <param name="Stderr">stderr if any</param>
        readonly record struct ExecResult(int Code = -1, string Stdout = "", string Stderr = "");
        #endregion

        #region Fields
        /// <summary>Simple profiling.</summary>
        readonly TimeIt _tmit = new();

        /// <summary>Where the exe lives.</summary>
        readonly string _exePath;

        /// <summary>Log file path.</summary>
        readonly string _logPath;

        // /// <summary>Debug file path.</summary>
        // readonly string _tracePath;

        /// <summary>Don't use reserved commands.</summary>
        readonly List<string> _reserved = ["edit", "explore", "find", "open", "print", "properties", "runas"];

        /// <summary>All the user commands.</summary>
        readonly List<ExplorerCommand> _explorerCommands = [];

        /// <summary>All the user commands.</summary>
        readonly List<RunCommand> _runCommands = [];
        #endregion

        #region Debug help
        // /// <summary>Dry run the registry writes.</summary>
        // readonly bool _fake = false;

        /// <summary>Flag set in ini config.</summary>
        bool _debug = false;

        /// <summary>Some behavior is altered if running in VS.</summary>
        readonly bool _inDev = false;
        #endregion

        #region The command line application
        /// <summary>Start here.</summary>
        /// <param name="args"></param>
        [STAThread]
        static void Main(string[] args)
        {
            // TODO? Keep in memory like ClipPlayer (Ipc.Server) or MassProcessingService.
            new App(args);
        }

        /// <summary>Do the work.</summary>
        /// <param name="appArgs"></param>
        public App(string[] appArgs)
        {
            int code = 0;

            try
            {
                _tmit.Snap("App constructor");

                ///// Init internal stuff.
                _inDev = Debugger.IsAttached;
                _exePath = Path.GetDirectoryName(Application.ExecutablePath)!;
//                _tracePath = Path.Join(_exePath, "shellinator.trc");
//                File.Delete(_tracePath); // reset


//                Console.BackgroundColor = ConsoleColor.Green;

                // Set up logging.
                _logPath = Path.Join(_exePath, "shellinator.log");
                FileInfo fi = new(_logPath);
                if (fi.Exists && fi.Length > 50000)
                {
                    var newfn = _logPath.Replace(".log", "_old.log");
                    File.Delete(newfn);
                    File.Move(_logPath, newfn);
                }
                Log($"==================== Running shellinator ====================");
                Log($"Command line [{string.Join("|", appArgs)}]");

                // Read configuration. Config file is assumed to be next to the executable.
                // If missing init with default. TODO1 allow arbitrary for dev?
                var cfn = Path.Combine(_exePath, "shellinator.ini");
                if (!File.Exists(cfn))
                {
                    Log($"Copying default.ini");
                    File.WriteAllBytes(cfn, Properties.Resources.default_ini);
                }
                _tmit.Snap("Start load config");
                LoadIni(cfn);
                _tmit.Snap("Done load config");

                ///// Process the args: shellinator.exe id context target. id is registry key or internal command name.
                var id = appArgs.Length > 0 ? appArgs[0] : "";
                var idlc = id.ToLower();
                var context = appArgs.Length > 1 ? appArgs[1] : null;
                var contextlc = context?.ToLower();
                var target = appArgs.Length > 2 ? appArgs[2] : null;
                var ext = target is null ? "" : Path.GetExtension(target).ToLower().Replace(".", "");

                switch (idlc, contextlc, target)
                {
                    ///// These are called by windows shell using registry entry. No console available. /////
                    case (_, "dir", not null):
                    case (_, "dirbg", not null):
                    case (_, "deskbg", not null):
                        {
                            var cmd = _explorerCommands.Where(c => c.Context == contextlc && c.Key == idlc).FirstOrDefault();
                            if (cmd is null) { throw new ShellinatorException($"Invalid command line"); }
                            ExecuteCommand(cmd.ExecLine, target);
                        }
                        break;

                    case ("run", _, not null):
                        {
                            var cmd = _runCommands.Where(c => c.Ext == ext).FirstOrDefault();
                            // Try default file?
                            cmd ??= _runCommands.Where(c => c.Ext == "*").FirstOrDefault();
                            if (cmd is null) { throw new ShellinatorException($"Invalid command line"); }
                            ExecuteCommand(cmd.ExecLine, target);
                        }
                        break;

                    ///// These are called from user console. /////
                    case ("list", null, null):
                        {
                            Tell("Registry commands:");
                            Tell(ListRegistryCommands());
                        }
                        break;

                    case ("reg", null, null):
                        {
                            Tell("Register all:");
                            _explorerCommands.ForEach(c => Tell(Register(c, false)));
                        }
                        break;

                    case ("unreg", null, null):
                        {
                            Tell("Unregister all:");
                            _explorerCommands.ForEach(c => Tell(Unregister(c, false)));
                        }
                        break;

                    case ("dev", null, null):
                        {
                            Tell("COMMANDS:");
                            _explorerCommands.ForEach(cmd => Tell(cmd.ToString()));
                            _runCommands.ForEach(cmd => Tell(cmd.ToString()));

                            // Trace reg calls without executing.
//                            _fake = true;
                            Tell("UNREG:");
                            _explorerCommands.ForEach(cmd => Tell(Unregister(cmd, true)));
                            Tell("REG:");
                            _explorerCommands.ForEach(cmd => Tell(Register(cmd, true)));
//                            _fake = false;

                            // Dump hive contents.
                            // Tell("UNREG:");
                            //res = DumpHive(RegistryHive.CurrentUser, @"Software\Classes");
                            //tell(res);
                            //DumpHive(RegistryHive.LocalMachine, @"Software\Classes");
                            //DumpHive(RegistryHive.CurrentUser, @"\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts");
                        }
                        break;

                    ///// Fail. /////
                    default:
                        throw new ShellinatorException($"Invalid command line args");
                }
            }
            catch (ShellinatorException ex) // app error
            {
                Log(ex);
                code = 2;
            }
            catch (Exception ex) // something else
            {
                Log(ex);
                code = 3;
            }

            _tmit.Snap("All done");
            //Trace(_tmit.Captures, "TMIT");

            Environment.Exit(code);
        }
        #endregion

        #region Internals
        /// <summary>
        /// Generic command executor.
        /// </summary>
        /// <param name="cmd">Command followed by all args.</param>
        /// <param name="target"></param>
        /// <returns>Result code</returns>
        int ExecuteCommand(List<string> cmd, string target)
        {
            Log($"ExecuteCommand() [{string.Join("|", cmd)}]");
            _tmit.Snap("Execute command start");

            // Do any replacements.
            var replArgs = cmd.Select(l =>
            {
                var s = l.Replace("$target", target);
                return Environment.ExpandEnvironmentVariables(s);
            }).ToList();

            ProcessStartInfo pinfo = new(replArgs[0], replArgs[1..])
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            // pinfo.EnvironmentVariables["MY_VAR"] = "Hello!";

            using Process proc = new() { StartInfo = pinfo };

            try
            {
                //LogInfo("Start process...");
                proc.Start();
            }
            catch (Exception ex)
            {
                Log(ex);
            }

            // TIL: To avoid deadlocks, always read the output stream first and then wait.
            var stdout = proc.StandardOutput.ReadToEnd();
            var stderr = proc.StandardError.ReadToEnd();

            // LogInfo("Wait for process to exit...");
            proc.WaitForExit();

            if (proc.ExitCode == 0)
            {
                // Success. Capture any stdout. TIL don't set clipboard to an empty string.
                if (stdout.Length > 0)
                {
                    Clipboard.SetText(stdout);
                }
            }
            else
            {
                // Command failed. Capture everything useful.
                List<string> ls = [ $"Command failed with code {proc.ExitCode}" ];

                if (stdout.Length != 0)
                {
                    ls.Add("==================== stdout ====================");
                    ls.Add(stdout);
                }
                if (stderr.Length != 0)
                {
                    ls.Add("==================== stderr ====================");
                    ls.Add(stderr);
                }

                var sres = string.Join(Environment.NewLine, ls);

                if (sres.Length > 0)
                {
                    Log(sres);
                    Clipboard.SetText(sres);
                }
            }

            _tmit.Snap("Execute command end");
            return proc.ExitCode;
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

            // Process sections.
            foreach (var sectName in inrdr.GetSectionNames())
            {
                var sectNameParts = sectName.SplitByTokens(" ");

                switch (sectNameParts[0].ToLower())
                {
                    case "config":
                        inrdr.GetValues(sectName).ForEach(kv =>
                        {
                            switch (kv.Key.ToLower())
                            {
                                case "debug": _debug = kv.Value == "1"; break;
                                default: break; // error?
                            }
                        });
                        break;

                    case "dir":
                    case "dirbg":
                    case "deskbg":
                        {
                            if (sectNameParts.Count < 2) throw new IniSyntaxException($"Invalid section name [{sectName}]", -1);

                            var ctxt = sectNameParts[0];
                            var cmd = sectNameParts[1].ToLower();
                            var sectVals = inrdr.GetValues(sectName);

                            if (!sectVals.TryGetValue("menu", out var menu)) throw new IniSyntaxException($"Missing menu item in section [{sectName}]", -1);
                            if (!sectVals.TryGetValue("exec", out var exec)) throw new IniSyntaxException($"Missing exec item in section [{sectName}]", -1);
                            if (_reserved.Contains(cmd)) { throw new ArgumentException($"Reserved command [{cmd}]"); }

                            // Good to go.
                            var execParts = exec.SplitByToken(",");
                            _explorerCommands.Add(new(ctxt, cmd, menu, execParts));
                        }
                        break;

                    case "run":
                        {
                            if (sectNameParts.Count < 2) throw new IniSyntaxException($"Invalid run args [{sectName}]", -1);

                            var extensions = sectNameParts[1..];

                            var sectVals = inrdr.GetValues(sectName);
                            if (!sectVals.TryGetValue("exec", out var exec)) throw new IniSyntaxException($"Missing exec item in section [{sectName}]", -1);

                            // Good to go.
                            var execParts = exec.SplitByToken(",");
                            sectNameParts[1..].ForEach(ext => _runCommands.Add(new(ext, execParts)));
                        }
                        break;

                    default:
                        throw new ArgumentException($"Invalid context: {sectName}");
                }
            }

            // Add the internal command if required.
            if (_runCommands.Count > 0)
            {
                _explorerCommands.Add(new("File", "run", "Run", []));
            }
        }
        #endregion

        #region Registry editing
        /// <summary>
        /// Write a user command to the registry.
        /// </summary>
        /// <param name="cmd">Which command</param>
        /// <param name="fake"></param>
        /// <returns>What was written</returns>
        List<string> Register(ExplorerCommand cmd, bool fake)
        {
            List<string> res = [];

            // Assemble command: shellinator_path id context target
            using var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            using var regRoot = hkcu.OpenSubKey(@"Software\Classes", writable: true);

            // Key names etc.
            var subkey1 = $"{GetRegPath(cmd.Context)}\\shell\\{cmd.Key}";
            var subkey2 = $"{subkey1}\\command";

            // Determine target tag based on origin. Dir/File/Folder => %D  DirBg/DeskBg => %W.
            var targetTag = cmd.Context.ToLower() switch
            {
                "dirbg" => "%W",
                "deskbg" => "%W",
                _ => "%D",
            };

            // All paths and macros that expand to paths must be wrapped in double quotes.
            var exec = Path.Join(_exePath, "shellinator.exe");
            var expCmd = $"\"{exec}\" {cmd.Key} {cmd.Context} \"{targetTag}\"";

            res.Add($"REG CreateSubKey({subkey1}) SetValue(MUIVerb, {cmd.Text})");
            res.Add($"REG CreateSubKey({subkey2}) SetValue(_, {expCmd})");

            if (!fake)
            {
                using var k1 = regRoot!.CreateSubKey(subkey1);
                k1.SetValue("MUIVerb", cmd.Text);

                using var k2 = regRoot!.CreateSubKey(subkey2);
                k2.SetValue("", expCmd);
            }

            return res;
        }

        /// <summary>
        /// Delete existing registry entry.
        /// </summary>
        /// <param name="cmd">Which command</param>
        /// <param name="fake"></param>
        /// <returns>What was written</returns>
        List<string> Unregister(ExplorerCommand cmd, bool fake)
        {
            List<string> res = [];

            using var hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            using var regRoot = hkcu.OpenSubKey(@"Software\Classes", writable: true);

            var ssubkey = $"{GetRegPath(cmd.Context)}\\shell\\{cmd.Key}";

            res.Add($"UNREG DeleteSubKeyTree({ssubkey})");

            if (!fake)
            {
                regRoot!.DeleteSubKeyTree(ssubkey, false);
            }

            return res;
        }

        /// <summary>
        /// Convert the internal enum to registry key.
        /// </summary>
        static string GetRegPath(string context)
        {
            return context.ToLower() switch
            {
                "dir" => "Directory",
                "dirbg" => "Directory\\Background",
                "deskbg" => "DesktopBackground",
                "file" => "*",
                _ => throw new ArgumentException("Impossible")
            };
        }
        #endregion

        #region Infrastructure
        /// <summary>
        /// Tell the user something.
        /// </summary>
        /// <param name="s"></param>
        void Tell(string s)
        {
            Console.WriteLine(s);
        }

        /// <summary>
        /// Tell the user something.
        /// </summary>
        /// <param name="ls"></param>
        void Tell(List<string> ls)
        {
            ls.ForEach(s => Tell(s));
        }

        /// <summary>
        /// Simple logging.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="line"></param>
        void Log(string msg, [CallerLineNumber] int line = -1)
        {
            File.AppendAllText(_logPath, $"{DateTime.Now:yyyy'-'MM'-'dd HH':'mm':'ss.fff} ({line}) {msg}{Environment.NewLine}");
        }

        /// <summary>
        /// Simple error logging.
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="line"></param>
        void Log(Exception ex, [CallerLineNumber] int line = -1)
        {
            File.AppendAllText(_logPath, $"{DateTime.Now:yyyy'-'MM'-'dd HH':'mm':'ss.fff} >>> ({line}) {ex}{Environment.NewLine}");
            MessageBox.Show(ex.ToString(), ex.Message);
        }

        ///// <summary>
        ///// Simple logging.
        ///// </summary>
        ///// <param name="msg"></param>
        ///// <param name="id"></param>
        ///// <param name="line"></param>
        //void Trace(string msg, string id, [CallerLineNumber] int line = -1)
        //{
        //    File.AppendAllText(_tracePath, $"{id}({line}) {msg}{Environment.NewLine}");
        //}

        ///// <summary>
        ///// Simple logging.
        ///// </summary>
        ///// <param name="msg"></param>
        ///// <param name="id"></param>
        ///// <param name="line"></param>
        //void Trace(List<string> msg, string id, [CallerLineNumber] int line = -1)
        //{
        //    var smsg = string.Join(Environment.NewLine, msg);
        //    File.AppendAllText(_tracePath, $"+++ {id} ({line}){Environment.NewLine}{smsg}{Environment.NewLine}");
        //}

        /// <summary>
        /// List the current shellinator registry commands.
        /// </summary>
        List<string> ListRegistryCommands()
        {
            List<string> res = [];

            using var hkcr = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            using var regRoot = hkcr.OpenSubKey(@"Software\Classes", writable: false);

            List<string> contexts = ["Directory", "Directory\\Background", "DesktopBackground", "*"];

            foreach (var ctx in contexts)
            {
                var sname = @$"{ctx}\shell";
                using var subkey = regRoot!.OpenSubKey(sname, writable: false);
                if (subkey is null) continue; // nothing of interest here

                foreach (string id in subkey.GetSubKeyNames()) // e.g. findev, run, ...
                {
                    try
                    {
                        using var subkey1 = subkey.OpenSubKey(id, writable: false);
                        var muiVerb = subkey1!.GetValue("MUIVerb");

                        using var subkey2 = subkey1.OpenSubKey("command", writable: false);
                        var scommand = subkey2!.GetValue("");

                        res.Add($"[{subkey1}]{Environment.NewLine}    [{muiVerb}] => [{scommand}]");

                    }
                    catch (Exception ex)
                    {
                        res.Add($">>> Something wrong with [{subkey}] [{id}] [{ex.Message}]");
                    }
                }
            }

            if (res.Count == 0)
            {
                res.Add("None");
            }

            return res;
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
                $"====================== {hive} {subkey} ======================",
            ];

            using var hkcr = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var regRoot = hkcr.OpenSubKey(subkey, writable: false);

            List<string> contexts = ["Directory", "Folder", "*"];
            string sind = "    ";
            contexts.ForEach(ctx => DoSubkey(regRoot!, ctx));

            // Local recursive func.
            void DoSubkey(RegistryKey key, string sname, int indent = 0)
            {
                string subind = string.Concat(Enumerable.Repeat(sind, indent));

                res.Add($"{subind}[{sname}]");

                using var subkey = key.OpenSubKey(sname, writable: false);
                if (subkey is null) return;

                foreach (string sval in subkey.GetValueNames())
                {
                    // "" means default
                    res.Add($"{subind}{sind}[{sval}]:[{subkey.GetValue(sval)}]");
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
