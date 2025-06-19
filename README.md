
# Shellinator
Playing with shell extensions to provide some custom context menus.

Consists of two parts:
- A simple command line client that is called from registry commands. It executes the requested operation.
- A UI companion that can configure registry commands, and some other stuff.

A few limitations that could be reconsidered:
- Operations on files are enabled generically, eventually specific extensions could be supported.
- If the commands produce output (stdout/stderr) it is placed in the clipboard for the user.

Built with VS2022 and .NET8.

# Commands

Commands vary depending on which part of the explorer they originate in.

| Context   | Description |
| -------   | ----------- |
| Dir       | Right click in explorer right pane or windows desktop with a directory selected.|
| DirBg     | Right click in explorer right pane with nothing selected (background).|
| DeskBg    | Right click in windows desktop with nothing selected (background).|
| Folder    | Right click in explorer left pane (navigation) with a folder selected.|
| File      | Right click in explorer right pane or windows desktop with a file selected.|


These are the builtin commands. It's stuff I've wanted to add to an explorer context menu.

| Menu Item          | Context   | Action |
| ---------          | ------    | ------ |
| Commander          | Dir       | Open a new explorer next to the current. Simulates old school Commander. |
| Tree               | Dir       | Copy a tree of selected directory to clipboard. |
| Open in Sublime    | Dir/DirBg | Open selected directory/here in Sublime Text. |
| Find in Everything | Dir/DirBg | Open selected directory/here in Everything. |
| Execute            | File      | Execute if executable otherwise open. Suppresses console window creation. |


# Implementation

## Registry Conventions

Registry sections of interest:
- `HKEY_LOCAL_MACHINE` (HKLM): defaults for all users using a machine (administrator)
- `HKEY_CURRENT_USER` (HKCU): user specific settings (not administrator)
- `HKEY_CLASSES_ROOT` (HKCR): virtual hive of `HKEY_LOCAL_MACHINE` with `HKEY_CURRENT_USER` overrides (administrator)

`HKEY_CLASSES_ROOT` should be used only for reading currently effective settings. A write to `HKEY_CLASSES_ROOT` is
always redirected to `HKEY_LOCAL_MACHINE`\Software\Classes. 

>>>>> In general, write directly to `HKEY_LOCAL_MACHINE\Software\Classes` or `HKEY_CURRENT_USER\Software\Classes` and read from `HKEY_CLASSES_ROOT`.

Shellinator bases all registry accesses (R/W) at `HKEY_CURRENT_USER\Software\Classes` aka `REG_ROOT`.


## Shellinator Commands

Shellinator command specifications contain these properties:

| Property      | Description |
| --------      | ----------- |
| Id            | Short name for internal id and registry key.|
| RegPath       | Where to install in `REG_ROOT`.|
| Text          | As it appears in the context menu.|
| CommandLine   | Full command string to execute.|


Supported `RegPath`s are:

| Context   | RegPath               |
| -------   | -------               |
| Dir       | HKCU\Directory             |
| DirBg     | HKCU\Directory\Background  |
| DeskBg    | HKCU\DesktopBackground     |
| Folder    | HKCU\Folder                |
| File      | HKCU\*                     |



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

| Macro     | Description | Notes |
| ----      | ----------- | ----- |
| %L        | Selected file or directory name. | Only Dir, File. | 
| %D        | Selected file or directory with expanded named folders. | Only Dir, File, Folder |
| %V        | The directory of the selection, maybe unreliable? | All except Folder. | 
| %W        | The working directory. | All except Folder. |
| %<0-9>    | Positional arg. |  |
| %*        | Replace with all parameters. |  |
| %~        | Replace with all parameters starting with the second parameter. |  |


Shellinator-specific macros:

| Macro     | Description |
| ----      | ----------- |
| %ID       | The Id property value. |
| %SHELLINATOR   | Path to the Shellinator executable. |

!! Note that all paths and macros that expand to paths must be wrapped in double quotes.

The usual env vars like `%ProgramFiles%` are also supported.

## Submenus

Not used currently in Shellinator but could be useful later.

Note!! Must use `MUIVerb`, not default value `@="text"`. A hard learn.

```ini
[HKCU\Directory]

[HKCU\Directory\shell]

[HKCU\Directory\shell\top_menu]
@=""
"MUIVerb"="Top Menu"
"subcommands"=""

[HKCU\Directory\shell\top_menu\shell]

[HKCU\Directory\shell\top_menu\shell\sub_menu_1]
@=""
"MUIVerb"="Sub Menu 1"

[HKCU\Directory\shell\top_menu\shell\sub_menu_1\command]
@="my_command.exe"

[HKCU\Directory\shell\top_menu\shell\sub_menu_2]
@=""
"MUIVerb"="Sub Menu 2"

[HKCU\Directory\shell\top_menu\shell\sub_menu_2\command]
@="my_command.exe"
```

# Refs

- General how to: https://learn.microsoft.com/en-us/windows/win32/shell/context-menu-handlers
- Detailed registry editing: https://mrlixm.github.io/blog/windows-explorer-context-menu/
- Shell command vars: https://superuser.com/questions/136838/which-special-variables-are-available-when-writing-a-shell-command-for-a-context
