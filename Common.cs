using System;
using System.Collections.Generic;


namespace Shellinator
{
    /// <summary>Internal exception.</summary>
    class ShellinatorException(string msg) : Exception(msg) { }

    /// <summary>Describes one menu command.</summary>
    /// <param name="Context">Where to install.</param>
    /// <param name="Key">Registry key/command OR file extension. Can't use reserved: edit, explore, find, open, print, properties, runas.</param>
    /// <param name="Text">As it appears in the context menu.</param>
    /// <param name="ExecLine">Command line args to execute.</param>
    record ExplorerCommand(string Context, string Key, string Text, List<string> ExecLine)
    {
        public override string ToString() { return $"Context:{Context} Key:{Key} Text:{Text} ExecLine:{string.Join("|", ExecLine)}"; }
    };

    /// <summary>Convenience container.</summary>
    /// <param name="Code">Return code</param>
    /// <param name="Stdout">stdout if any</param>
    /// <param name="Stderr">stderr if any</param>
    readonly record struct ExecResult(int Code = -1, string Stdout = "", string Stderr = "");
}
