# Splunk
Games with shell extensions to provide some custom context menus.

Consists of two parts:
- A server (Splunk) that performs the actual work. It's out-of-process from the client
  so as to not inadvertently compromise the entire shell.
- A simple command line client that is called from registry commands. It talks to the
  server via a named pipe.

Built with VS2022 and .NET8.


# Commands

## General

Registry sections of interest:
- `HKEY_LOCAL_MACHINE` (HKLM): defaults for all users using a machine (administrator)
- `HKEY_CURRENT_USER` (HKCU): user specific settings (not administrator)
- `HKEY_CLASSES_ROOT` (HKCR): virtual hive of `HKEY_LOCAL_MACHINE` with `HKEY_CURRENT_USER` overrides (administrator)

`HKEY_CLASSES_ROOT` should be used only for reading currently effective settings. A write to `HKEY_CLASSES_ROOT` is
always redirected to `HKEY_LOCAL_MACHINE`\Software\Classes. In general, write directly to 
`HKEY_LOCAL_MACHINE\Software\Classes` or `HKEY_CURRENT_USER\Software\Classes` and read from `HKEY_CLASSES_ROOT`.

In subsequent sections, `HKEY_XX` is `HKEY_CURRENT_USER\Software\Classes`.

Arguments available in commands executed by registry handlers:

| Main | Description |
| ---- | ----------- |
| %L   | Long file name form of the command. |
| %D   | Desktop absolute parsing name of the command (for items that don't have file system paths). |
| %V   | For verbs that are none implies all. If there is no parameter passed this is the working directory. |
| %W   | The working directory. |
| %N   | The positional args. |


## Splunk Commands

All client commands are of the form:
```
"CL_PATH\SplunkClient.exe" "command" "%V"
```


| Command | Description | Applies To |
| -----   | ------      | -----      |
| cmder   | Open a second explorer in dir - aligned with first.   | HKEY_XX\Directory |
| tree    | Cmd line to clipboard for current or sel dir.         | HKEY_XX\Directory, HKEY_XX\Directory\Background |
| newtab  | Open dir in new tab in current explorer.              | HKEY_XX\Directory, HKEY_XX\DesktopBackground |
| openst  | Open dir in Sublime Text.                             | HKEY_XX\Directory, HKEY_XX\Directory\Background |
| find    | Open dir in Everything.                               | HKEY_XX\Directory, HKEY_XX\Directory\Background |


## Registry Keys Of Interest

Not shown are attributes like icon, extended, ...

```ini
; => Right click in explorer-right-pane or windows-desktop with a directory selected.
[HKEY_XX\Directory\shell\menu_item]
@=""
"MUIVerb"="Menu Item"
;The command to execute - arg is the directory.
[HKEY_XX\Directory\shell\menu_item\command]
@="my_command.exe" "%V"
```

```ini
; => Right click in explorer-right-pane with nothing selected (background)
[HKEY_XX\Directory\Background\shell\menu_item]
@=""
"MUIVerb"="Menu Item"
;The command to execute - arg is not used.
[HKEY_XX\Directory\Background\shell\menu_item\command]
@="my_command.exe" "%V"
```

```ini
; => Right click in windows-desktop with nothing selected (background).
[HKEY_XX\DesktopBackground\shell\menu_item]
@=""
"MUIVerb"="Menu Item"
;The command to execute - arg is not used.
[HKEY_XX\DesktopBackground\shell\menu_item\command]
@="my_command.exe" "%V"
```

```ini
; => Right click in explorer-left-pane (navigation) with a folder selected.
[HKEY_XX\Folder\shell\menu_item]
@=""
"MUIVerb"="Menu Item"
;The command to execute - arg is the folder.
[HKEY_XX\Folder\shell\menu_item\command]
@="my_command.exe" "%V"
```

```ini
; => Right click in explorer-right-pane or windows-desktop with a file selected (* for all exts).
[HKEY_XX\*\shell\menu_item]
@=""
"MUIVerb"="Menu Item"
;The command to execute - arg is the file name.
[HKEY_XX\*\shell\menu_item\command]
@="my_command.exe" "%V"
```

## Submenus

Typical structure of submenu entries.

Note!! Human names must use `MUIVerb`, not default value `@="text"`. A hard learn.

```ini
[HKEY_XX\Directory]

[HKEY_XX\Directory\shell]

[HKEY_XX\Directory\shell\top_menu]
@=""
"MUIVerb"="Top Menu"
"subcommands"=""

[HKEY_XX\Directory\shell\top_menu\shell]

[HKEY_XX\Directory\shell\top_menu\shell\sub_menu_1]
@=""
"MUIVerb"="Sub Menu 1"

[HKEY_XX\Directory\shell\top_menu\shell\sub_menu_1\command]
@="my_command.exe"

[HKEY_XX\Directory\shell\top_menu\shell\sub_menu_2]
@=""
"MUIVerb"="Sub Menu 2"

[HKEY_XX\Directory\shell\top_menu\shell\sub_menu_2\command]
@="my_command.exe"
```

# Refs

- Detailed registry editing: https://mrlixm.github.io/blog/windows-explorer-context-menu/
- Args: https://superuser.com/questions/136838/which-special-variables-are-available-when-writing-a-shell-command-for-a-context
