
# Splunk
Games with shell extensions to provide some custom context menus.

Consists of two parts:
- A simple command line client that is called from registry commands. It executes the
  requested operation.
- A UI companion that can configure registry commands, and some other stuff.

Built with VS2022 and .NET8.

TODO2 publishing and packaging: https://stackoverflow.com/questions/58994946/how-to-build-app-without-app-runtimeconfig-json

# Commands

## Registry Conventions

Registry sections of interest:
- `HKEY_LOCAL_MACHINE` (HKLM): defaults for all users using a machine (administrator)
- `HKEY_CURRENT_USER` (HKCU): user specific settings (not administrator)
- `HKEY_CLASSES_ROOT` (HKCR): virtual hive of `HKEY_LOCAL_MACHINE` with `HKEY_CURRENT_USER` overrides (administrator)

`HKEY_CLASSES_ROOT` should be used only for reading currently effective settings. A write to `HKEY_CLASSES_ROOT` is
always redirected to `HKEY_LOCAL_MACHINE`\Software\Classes. In general, write directly to 
`HKEY_LOCAL_MACHINE\Software\Classes` or `HKEY_CURRENT_USER\Software\Classes` and read from `HKEY_CLASSES_ROOT`.

Splunk bases all registry accesses (R/W) at `HKEY_CURRENT_USER\Software\Classes` aka `REG_ROOT`.


## Splunk Commands

Splunk command specifications contain these properties:

| Property      | Description |
| --------      | ----------- |
| Id            | Short name for internal id and registry key.|
| RegPath       | Where to install in `REG_ROOT`.|
| Text          | As it appears in the context menu.|
| CommandLine   | Full command string to execute.|


Supported `RegPath`s are:

| RegPath               | Description |
| -------               | ----------- |
| Directory             | Right click in explorer right pane or windows desktop with a directory selected.|
| Directory\Background  | Right click in explorer right pane with nothing selected (background).|
| DesktopBackground     | Right click in windows desktop with nothing selected (background).|
| Folder                | Right click in explorer left pane (navigation) with a folder selected.|
| ext                   | Right click in explorer right pane or windows desktop with file.ext selected (\* for all exts).|


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
| %L        | Selected file or directory name. | All except folder. | 
| %D        | Selected file or directory with expanded named folders. | Dir, file, folder |
| %V        | The directory of the selection, maybe but unreliable. | All except folder. | 
| %W        | The working directory. | All except folder. |
| %<0-9>    | Positional arg. |  |
| %*        | Replace with all parameters. |  |
| %~        | Replace with all parameters starting with the second parameter. |  |


TODO2 clean this up.


See: https://superuser.com/questions/136838/which-special-variables-are-available-when-writing-a-shell-command-for-a-context


cmd.exe /k "echo DIR %L %D %V %W"  test103
cmd.exe /k "echo DIRBG %V %W"   D->crash L->ignored  test102
cmd.exe /k "echo DESKBG %V %W"  test104
cmd.exe /k "echo FILE %L %D %V %W"  test105
cmd.exe /k "echo FOLDER %D"  test101
>>>
in C:\Dev:
DIR %L=C:\Dev\repos %D=C:\Dev\repos %V=C:\Dev\repos %W=C:\Dev -- use %D
DIRBG %V=C:\Dev %W=C:\Dev -- use %W
DESKBG %V=C:\Users\cepth\Desktop %W=C:\Users\cepth\Desktop -- use %W
FILE %L=C:\Dev\_cmd_out.txt %D=C:\Dev\_cmd_out.txt %V=C:\Dev\_cmd_out.txt %W=C:\Dev -- use %L
FOLDER C:\Users\cepth\Desktop (rt pane) -- TODO2 don't use folder for now, strange behavior
  or FOLDER ::{F874310E-B6B7-47DC-BC84-B9E6B38F5903} (left pane -> Home)
  when left pane is a folder (default home) click on a right pane selected dir shows both 103 and 101. 103 gives an error.


Splunk-specific macros:

| Macro     | Description |
| ----      | ----------- |
| %ID       | The Id property value. |
| %SPLUNK   | Path to the Splunk executable. |

!! Note that all paths and macros that expand to paths must be wrapped in double quotes.

## Submenus

Not used currently in Splunk but could be useful later.

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

- Detailed registry editing: https://mrlixm.github.io/blog/windows-explorer-context-menu/
