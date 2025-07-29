using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
//using WI = Ephemera.Win32.Internals;
//using CB = Ephemera.Win32.Clipboard;


namespace Shellinator
{
    /// <summary>The customized part of the app.</summary>
    internal partial class App
    {
        /// <summary>Set up commands.</summary>
        void InitCommands()
        {
            _commands =
            [
                new("treex",  ExplorerContext.Dir,    "Treex Dir",              "Copy a tree of selected directory to clipboard",   TreexCmd),
                new("treex",  ExplorerContext.DirBg,  "Treex DirBg",              "Copy a tree here to clipboard.",                   TreexCmd),
                new("openst", ExplorerContext.Dir,    "Open in Sublime Dir",    "Open selected directory in Sublime Text.",         SublimeCmd),
                new("openst", ExplorerContext.DirBg,  "Open in Sublime DirBg",    "Open here in Sublime Text.",                       SublimeCmd),
                new("findev", ExplorerContext.Dir,    "Open in Everything Dir", "Open selected directory in Everything.",           EverythingCmd),
                new("findev", ExplorerContext.DirBg,  "Open in Everything DirBg", "Open here in Everything.",                         EverythingCmd),
                new("exec",   ExplorerContext.File,   "Execute File",            "Execute file if executable otherwise open it.",    ExecCmd),
                //new("test",   ExplorerContext.Dir,    "==Test Dir==",       "Debug stuff.",                                     TestCmd),
                //new("test",   ExplorerContext.DirBg,  "==Test DirBg==",     "Debug stuff.",                                     TestCmd),
                //new("test",   ExplorerContext.DeskBg, "==Test DeskBg==",    "Debug stuff.",                                     TestCmd),
                //new("test",   ExplorerContext.File,   "==Test File==",      "Debug stuff.",                                     TestCmd),
                ///////////new("test",   ExplorerContext.Folder, "==Test Folder==",    "Debug stuff.",                                     TestCmd)
            ];
        }
        //Old version:
        //new ("tree", ExplorerContext.Dir, "Tree", "%SPLUNK %ID \"%D\"", "Copy a tree of selected directory to clipboard"),
        //new ("tree", ExplorerContext.DirBg, "Tree", "%SPLUNK %ID \"%W\"", "Copy a tree here to clipboard."),
        //new ("openst", ExplorerContext.Dir, "Open in Sublime", "\"%ProgramFiles%\\Sublime Text\\subl\" --launch-or-new-window \"%D\"", "Open selected directory in Sublime Text."),
        //new ("openst", ExplorerContext.DirBg, "Open in Sublime", "\"%ProgramFiles%\\Sublime Text\\subl\" --launch-or-new-window \"%W\"", "Open here in Sublime Text."),
        //new ("findev", ExplorerContext.Dir, "Find in Everything", "%ProgramFiles%\\Everything\\everything -parent \"%D\"", "Open selected directory in Everything."),
        //new ("findev", ExplorerContext.DirBg, "Find in Everything", "%ProgramFiles%\\Everything\\everything -parent \"%W\"", "Open here in Everything."),
        //new ("exec", ExplorerContext.File, "Execute", "%SPLUNK %ID \"%D\"", "Execute file if executable otherwise opened."),
        //new ("test_deskbg", ExplorerContext.DeskBg, "!! Test DeskBg", "%SPLUNK %ID \"%W\"", "Debug stuff."),
        //new ("test_folder", ExplorerContext.Folder, "!! Test Folder", "%SPLUNK %ID \"%D\"", "Debug stuff."),



        //--------------------------------------------------------//
        ExecResult TreexCmd(ExplorerContext context, string target)
        {
            var res = context switch
            {
                //ExplorerContext.Dir => ExecuteCommand("cmd", ["/C", "tree", target]),
               // ExplorerContext.Dir => ExecuteCommand("cmd", ["/C", "treex", "-c", target, "| clip"]),
                ExplorerContext.Dir => ExecuteCommand("tree", [target, "| clip"]),
                ExplorerContext.DirBg => ExecuteCommand("cmd", ["/C", "treex", "-c", target, "| clip"]),
                _ => throw new ShellinatorException($"Invalid context: {context}"),
            };

            Notify(res.Stdout??"null");

            return res;
        }

        //--------------------------------------------------------//
        ExecResult SublimeCmd(ExplorerContext context, string target)
        {
            var stpath = Path.Join(Environment.ExpandEnvironmentVariables("%ProgramFiles%"), "Sublime Text", "subl");

            return new();


            var res = context switch
            {
                ExplorerContext.Dir => ExecuteCommand("cmd", ["/C", stpath, "--launch-or-new-window", target]),// sel),
                ExplorerContext.DirBg => ExecuteCommand("cmd", ["/C", stpath, "--launch-or-new-window", target]),// wdir),
                //ExplorerContext.Dir => ExecuteCommand($"\"{stpath}\"", $"--launch-or-new-window \"{wdir}\"", sel),
                //ExplorerContext.DirBg => ExecuteCommand("cmd", $"/C \"{stpath}\" --launch-or-new-window \"{wdir}\"", wdir),
                //ExplorerContext.DirBg => ExecuteCommand($"\"{stpath}\"", $"--launch-or-new-window \"{wdir}\"", wdir),
                _ => throw new ShellinatorException($"Invalid context: {context}"),
            };

            return res;
        }

        //--------------------------------------------------------//
        ExecResult EverythingCmd(ExplorerContext context, string target)
        {
            var evpath = Path.Join(Environment.ExpandEnvironmentVariables("%ProgramFiles%"), "Everything", "everything");

            // var res = context switch
            // {
            //     ExplorerContext.Dir => ExecuteCommand("cmd", $"/C \"{evpath}\" \"{wdir}\"", sel),
            //     ExplorerContext.DirBg => ExecuteCommand("cmd", $"/C \"{evpath}\" \"{wdir}\"", wdir),
            //     _ => throw new ShellinatorException($"Invalid context: {context}"),
            // };

            // return res;
            return new();
        }

        //--------------------------------------------------------//
        ExecResult ExecCmd(ExplorerContext context, string target)
        {
            var ext = Path.GetExtension(target);

            // var res = ext switch
            // {
            //     ".cmd" or ".bat" => ExecuteCommand("cmd", $"/C \"{sel}\"", wdir),
            //     ".ps1" => ExecuteCommand("powershell", $"-executionpolicy bypass -File \"{sel}\"", wdir),
            //     ".lua" => ExecuteCommand("lua", $"\"{sel}\"", wdir),
            //     ".py" => ExecuteCommand("python", $"\"{sel}\"", wdir),
            //     _ => ExecuteCommand("cmd", $"/C \"{sel}\"", wdir) // default just open.
            // };

            // return res;
            return new();
        }

        //--------------------------------------------------------//
        ExecResult TestCmd(ExplorerContext context, string target)
        {
            Log($"!!! Test context:[{context}] target:[{target}]");// wdir:[{wdir}]");
            return new();
        }
    }
}
