using System;


namespace Shellinator
{
    /// <summary>Internal exception.</summary>
    class ShellinatorException(string msg) : Exception(msg) { }

    /// <summary>
    /// Commands vary depending on which part of explorer they originate in. These are supported.
    /// Operations on files are enabled generically, eventually specific extensions could be supported.
    /// </summary>
    enum ExplorerContext
    {
        /// <summary>Right click in explorer with a directory selected.</summary>
        Dir,
        /// <summary>Right click in explorer right pane with nothing selected (background).</summary>
        DirBg,
        /// <summary>Right click in windows desktop with nothing selected (background).</summary>
        DeskBg,
        /// <summary>Right click in explorer with a file selected.</summary>
        File,
        ///// <summary>Seems to appear for any directory selection. Probably meant for system use.</summary>
        //Folder,
    }

    /// <summary>Describes one menu command.</summary>
    /// <param name="Id">Internal id and registry key. Don't use: edit, explore, find, open, print, properties, runas.</param>
    /// <param name="Context">Where to install in REG_ROOT</param>
    /// <param name="Text">As it appears in the context menu.</param>
    /// <param name="Description">As it appears in the context menu.</param>
    /// <param name="Handler">Handle command.</param>
    readonly record struct ExplorerCommand(string Id, ExplorerContext Context, string Text, string Description, CommandHandler Handler);

    /// <summary>Command handler.</summary>
    /// <param name="context">ExplorerContext</param>
    /// <param name="target">Selected item</param>
    delegate ExecResult CommandHandler(ExplorerContext context, string target);

    /// <summary>Convenience container.</summary>
    /// <param name="Code">Return code</param>
    /// <param name="Stdout">stdout if any</param>
    /// <param name="Stderr">stderr if any</param>
    readonly record struct ExecResult(int Code = -1, string Stdout = "", string Stderr = "");
}
