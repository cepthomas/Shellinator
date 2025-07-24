
# Shellinator
Playing with shell extensions to provide some custom context menus.

Consists of a simple command line client that is called from registry commands. It executes the requested operation.
It is also used to configure registry commands, and some other stuff.

If the command fails, code/stdout/stderr is logged and user gets a message box.

Caveats:
- Operations on files are enabled generically, eventually specific extensions could be supported.

Built with VS2022 and .NET8.

To build this requires an env var named `DEV_BIN_PATH` which points to a directory to house the application.

# Commands

These are the builtin commands. What I consider useful. YMMV.

| Menu Item          | Context   | Action
| ---------          | ------    | ------
| Treex              | Dir       | Copy a tree of selected directory to clipboard
| Open in Sublime    | Dir/DirBg | Open selected directory/here in Sublime Text
| Open in Everything | Dir/DirBg | Open selected directory/here in Everything
| Execute            | File      | Execute if executable otherwise open. Suppresses console window creation


Commands vary depending on which part of the explorer they originate in. These are supported:

| Context   | Description
| -------   | -----------
| Dir       | Right click in explorer right pane or windows desktop with a directory selected.
| DirBg     | Right click in explorer right pane with nothing selected (background).
| DeskBg    | Right click in windows desktop with nothing selected (background).
| Folder    | Right click in explorer left pane (navigation) with a folder selected.
| File      | Right click in explorer right pane or windows desktop with a file selected.


Shellinator command specifications contain these properties:

| Property      | Description
| --------      | -----------
| Id            | Short name for internal id and registry key
| RegPath       | Where to install in `REG_ROOT`
| Text          | As it appears in the context menu
| CommandLine   | Full command string to execute


This generates registry entries that look like:
```ini
[REG_ROOT\spec.RegPath\shell\spec.Id]
@=""
"MUIVerb"=spec.Text
[REG_ROOT\spec.RegPath\shell\Id\command]
@=spec.CommandLine
```

`CommandLine` is a free-form string that is executed as if entered at the command line.
There are some macros available.

Built in macros:

| Macro     | Description                                                       | Notes
| ----      | -----------                                                       | -----
| %L        | Selected file or directory name                                   | Only Dir, File
| %D        | Selected file or directory with expanded named folders            | Only Dir, File, Folder
| %V        | The directory of the selection, maybe unreliable?                 | All except Folder
| %W        | The working directory                                             | All except Folder
| %<0-9>    | Positional arg                                                    |
| %*        | Replace with all parameters                                       |
| %~        | Replace with all parameters starting with the second parameter    |


Shellinator-specific macros:

| Macro         | Description
| ----          | -----------
| %ID           | The Id property value
| %SHELLINATOR  | Path to the Shellinator executable

!! Note that all paths and macros that expand to paths must be wrapped in double quotes.

The builtin env vars like `%ProgramFiles%` are also supported.


# Info

Registry sections of interest:
- `HKEY_LOCAL_MACHINE` (HKLM): defaults for all users using a machine (administrator)
- `HKEY_CURRENT_USER` (HKCU): user specific settings (not administrator)
- `HKEY_CLASSES_ROOT` (HKCR): virtual hive of `HKEY_LOCAL_MACHINE` with `HKEY_CURRENT_USER` overrides (administrator)

`HKEY_CLASSES_ROOT` should be used only for reading currently effective settings. A write to `HKEY_CLASSES_ROOT` is
always redirected to `HKEY_LOCAL_MACHINE`\Software\Classes. 

In general, write directly to `HKEY_LOCAL_MACHINE\Software\Classes` or `HKEY_CURRENT_USER\Software\Classes` and read from `HKEY_CLASSES_ROOT`.

Shellinator bases all registry accesses (R/W) at `HKEY_CURRENT_USER\Software\Classes` aka `REG_ROOT`.

- General how to: https://learn.microsoft.com/en-us/windows/win32/shell/context-menu-handlers
- Detailed registry editing: https://mrlixm.github.io/blog/windows-explorer-context-menu/
- Shell command vars: https://superuser.com/questions/136838/which-special-variables-are-available-when-writing-a-shell-command-for-a-context
