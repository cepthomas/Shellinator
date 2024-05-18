# Splunk
Games with shell extensions to provide some custom context menus.

Consists of two parts:
- A server (Splunk) that performs the actual work. It's out-of-process from the client
  so as to not inadvertently compromise the entire shell.
- A simple command line client that is called from registry commands. It talks to the
  server via a named pipe.

Built with VS2022 and .NET8.



# Splunk Commands

Read [Registry Notes] first.


All client commands are of the form:
```
"CL_PATH\SplunkClient.exe" command "%V"
```

| Command  | Description | Applies To |
| -----    | ------      | -----      |
| "cmder"  | Open a second XP in dir - aligned with first. option for full screen?      | HKXX\Directory      |
| "tree"   | Cmd line to clipboard for current or sel dir. Also dir submenu?      | HKXX\Directory, HKXX\Directory\Background      |
| "newtab" | Open dir in tab in current explorer. (middle button?)     | HKXX\Directory, HKXX\DesktopBackground      |
| "stdir"  | Open dir in a new Sublime. | HKXX\Directory, HKXX\Directory\Background      |


Maybe?
- find file in folder. Use everything?                        
- fix nav bar + history. (re)Implement?                         
- tag files/dirs. Use builtin libraries and/or favorites?
- more info/hover?. filters, fullpath, size, thumbnail     


# Registry Notes

## General

- Detailed registry editing: https://mrlixm.github.io/blog/windows-explorer-context-menu/
shellex entries? HKCU\Software\Classes\*\ShellEx

HKLM: HKEY_LOCAL_MACHINE  defaults for all users using a machine (requires administrator)
HKCU: HKEY_CURRENT_USER  user specific settings ==> use for everything!!
HKCR: HKEY_CLASSES_ROOT  virtual hive of HKLM with HKCU overrides (requires administrator)

HKCR should be used only for reading currently effective settings. A write to HKCR is always redirected to HKLM\Software\Classes. A write access to any key in HKLM requires administrator and HKCU do not. In general. write directly to either HKLM\Software\Classes or to HKCU\Software\Classes and read from HKCR.

HKXX can be:
- HKLM\SOFTWARE\Classes
- HKCU\Software\Classes ==> use this
- HKCR

## Command replacement params

https://superuser.com/questions/136838/which-special-variables-are-available-when-writing-a-shell-command-for-a-context

| Main | Description |
|----  | ----        |
| %L   | Long file name form of the command. |
| %D   | Desktop absolute parsing name of the command (for items that don't have file system paths). |
| %V   | For verbs that are none implies all. If there is no parameter passed this is the working directory. |
| %W   | The working directory. |
| %N   | The positional args. |


## Registry keys of interest

Not shown are things like icon, extended, ...

```
;Right click in explorer_right_pane or windows_desktop with a directory selected.
[HKEY_CURRENT_USER\Software\Classes\Directory\shell\menu_item]
@=""
"MUIVerb"="Menu Item"

;The command to execute - arg is the directory.
[HKEY_CURRENT_USER\Software\Classes\Directory\shell\menu_item\command]
@="my_command.exe" "%V"
```
```
;Right click in explorer_right_pane with nothing selected (background)
[HKEY_CURRENT_USER\Software\Classes\Directory\Background\shell\menu_item]
@=""
"MUIVerb"="Menu Item"

;The command to execute - arg is not used.
[HKEY_CURRENT_USER\Software\Classes\Directory\Background\shell\menu_item\command]
@="my_command.exe" "%V"
```
```
;Right click in windows_desktop with nothing selected (background).
[HKEY_CURRENT_USER\Software\Classes\DesktopBackground\shell\menu_item]
@=""
"MUIVerb"="Menu Item"

;The command to execute - arg is not used.
[HKEY_CURRENT_USER\Software\Classes\DesktopBackground\shell\menu_item\command]
@="my_command.exe" "%V"
```
```
;Right click in explorer_left_pane (navigation) with a folder selected.
[HKEY_CURRENT_USER\Software\Classes\Folder\shell\menu_item]
@=""
"MUIVerb"="Menu Item"

;The command to execute - arg is the folder.
[HKEY_CURRENT_USER\Software\Classes\Folder\shell\menu_item\command]
@="my_command.exe" "%V"
```
```
;Right click in explorer_right_pane or windows_desktop with a file selected (* for all exts).
[HKEY_CURRENT_USER\Software\Classes\*\shell\menu_item]
@=""
"MUIVerb"="Menu Item"

;The command to execute - arg is the file name.
[HKEY_CURRENT_USER\Software\Classes\*\shell\menu_item\command]
@="my_command.exe" "%V"
```


## Submenus

Note!! Must use MUIVerb not default value (@="")

```
[HKEY_CURRENT_USER\Software\Classes\Directory]

[HKEY_CURRENT_USER\Software\Classes\Directory\shell]

[HKEY_CURRENT_USER\Software\Classes\Directory\shell\top_menu]
@=""
"MUIVerb"="Top Menu"
"subcommands"=""

[HKEY_CURRENT_USER\Software\Classes\Directory\shell\top_menu\shell]

[HKEY_CURRENT_USER\Software\Classes\Directory\shell\top_menu\shell\sub_menu_1]
@=""
"MUIVerb"="Sub Menu 1"

[HKEY_CURRENT_USER\Software\Classes\Directory\shell\top_menu\shell\sub_menu_1\command]
@="my_command.exe"

[HKEY_CURRENT_USER\Software\Classes\Directory\shell\top_menu\shell\sub_menu_2]
@=""
"MUIVerb"="Sub Menu 2"

[HKEY_CURRENT_USER\Software\Classes\Directory\shell\top_menu\shell\sub_menu_2\command]
@="my_command.exe"
```

