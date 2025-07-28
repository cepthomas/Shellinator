using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;


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
                new("treex",  ExplorerContext.Dir,    "Treex",              "Copy a tree of selected directory to clipboard",   TreexCmd),
                new("treex",  ExplorerContext.DirBg,  "Treex",              "Copy a tree here to clipboard.",                   TreexCmd),
                new("openst", ExplorerContext.Dir,    "Open in Sublime",    "Open selected directory in Sublime Text.",         SublimeCmd),
                new("openst", ExplorerContext.DirBg,  "Open in Sublime",    "Open here in Sublime Text.",                       SublimeCmd),
                new("findev", ExplorerContext.Dir,    "Open in Everything", "Open selected directory in Everything.",           EverythingCmd),
                new("findev", ExplorerContext.DirBg,  "Open in Everything", "Open here in Everything.",                         EverythingCmd),
                new("exec",   ExplorerContext.File,   "Execute",            "Execute file if executable otherwise open it.",    ExecCmd),
                new("test",   ExplorerContext.Dir,    "==Test Dir==",       "Debug stuff.",                                     TestCmd),
                new("test",   ExplorerContext.DirBg,  "==Test DirBg==",     "Debug stuff.",                                     TestCmd),
                new("test",   ExplorerContext.DeskBg, "==Test DeskBg==",    "Debug stuff.",                                     TestCmd),
                new("test",   ExplorerContext.File,   "==Test File==",      "Debug stuff.",                                     TestCmd),
                new("test",   ExplorerContext.Folder, "==Test Folder==",    "Debug stuff.",                                     TestCmd)
            ];
        }
        //Dir - %D  DirBg - %W  File - %D  DeskBg - %W  Folder - %D
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
        ExecResult TreexCmd(ExplorerContext context, string target)//, string wdir)
        {
            var res = context switch
            {
                ExplorerContext.Dir => ExecuteCommand("cmd", ["/C", "treex", "-c", target, "| clip"]),//, sel),
                ExplorerContext.DirBg => ExecuteCommand("cmd", ["/C", "treex", "-c", target, "| clip"]),//, wdir),
                _ => throw new ShellinatorException($"Invalid context: {context}"),
            };

            return res;
        }

        //--------------------------------------------------------//
        ExecResult SublimeCmd(ExplorerContext context, string target)//, string wdir)
        {
            var stpath = Path.Join(Environment.ExpandEnvironmentVariables("%ProgramFiles%"), "Sublime Text", "subl");

            //Console.WriteLine($"{context}  \"{stpath}\" --launch-or-new-window \"{wdir}\"");
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
        ExecResult EverythingCmd(ExplorerContext context, string target)//, string wdir)
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
        ExecResult ExecCmd(ExplorerContext context, string target)//, string wdir)
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
        ExecResult TestCmd(ExplorerContext context, string target)//, string wdir)
        {
            ///// <summary>Right click in explorer right pane or windows desktop with a directory selected.</summary>
            //Dir = 0x01,
            ///// <summary>Right click in explorer right pane with nothing selected (background).</summary>
            //DirBg = 0x02,
            ///// <summary>Right click in windows desktop with nothing selected (background).</summary>
            //DeskBg = 0x04,
            ///// <summary>Right click in explorer left pane (navigation) with a folder selected.</summary>
            //Folder = 0x08,
            ///// <summary>Right click in explorer right pane or windows desktop with a file selected.</summary>
            //File = 0x10

            Log($"!!! Test context:[{context}] target:[{target}]");// wdir:[{wdir}]");
            return new();
        }
    }
}
