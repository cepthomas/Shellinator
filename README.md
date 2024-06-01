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

| Macro     | Description |
| ----      | ----------- |
| %L        | Selected file or directory name. |
| %D        | Desktop absolute parsing name of the selection for items that don't have file system paths. |
| %V        | The directory of the selection. |
| %W        | The working directory. |
| %<0-9>    | Positional arg. |
| %*        | Replace with all parameters. |
| %~        | Replace with all parameters starting with the second parameter. |
| %S        | Show command. |

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
- Args: https://superuser.com/questions/136838/which-special-variables-are-available-when-writing-a-shell-command-for-a-context
