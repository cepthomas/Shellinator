# Splunk
Games with shell extensions to provide some custom context menus.

Consists of two parts:
- A server (Splunk) that performs the actual work. It's out-of-process from the client
  so as to not inadvertently compromise the entire shell.
- A simple command line client that is called from registry commands. It talks to the
  server via a named pipe.

Built with VS2022 and .NET8.


# Registry

- Detailed registry editing: https://mrlixm.github.io/blog/windows-explorer-context-menu/
? HKCU\Software\Classes\*\ShellEx


HKLM: HKEY_LOCAL_MACHINE  defaults for all users using a machine (requires administrator)
HKCU: HKEY_CURRENT_USER  user specific settings
HKCR: HKEY_CLASSES_ROOT  virtual hive of HKLM with HKCU overrides (requires administrator)

HKCR should be used only for reading currently effective settings. A write to HKCR is always redirected to HKLM\Software\Classes. A write access to any key in HKLM requires administrator and HKCU do not. In general. write directly to either HKLM\Software\Classes or to HKCU\Software\Classes and read from HKCR.

Use HKEY_LOCAL_MACHINE or HKEY_CURRENT_USER

Windows explorer events of interest and their corresponding registry keys.

Event        | Description
----------- | -----------
example.neb | Source file showing example of static sequence and loop definitions, and creating notes by script functions.

Event origins:
- listing: Explorer right pane
- navigation: Explorer left pane
- desktop: Windows desktop

HKXX can be:
- HKLM\SOFTWARE\Classes
- HKCU\Software\Classes *** use this
- HKCR

HKEY_CURRENT_USER\Software\Classes
HKEY_LOCAL_MACHINE\Software\Classes

HKXX\Directory\shell\cmd_name -> Right click in listing or desktop with directory selected
HKXX\Directory\shell\cmd_name\command -> The command to execute - arg is directory

HKXX\*\shell\cmd_name -> Right click in listing or desktop with file selected (* for all exts)
HKXX\Directory\shell\cmd_name\command -> The command to execute - arg is file

HKXX\Directory\Background\shell\cmd_name -> Right click in listing with nothing selected (background)
HKXX\Directory\Background\shell\cmd_name\command -> The command to execute - no arg

HKXX\DesktopBackground\shell\cmd_name -> Right click in desktop with nothing selected (background)
HKXX\DesktopBackground\shell\cmd_name\command -> The command to execute - no arg

HKXX\Folder\shell\cmd_name -> Right click in navigation with folder selected
HKXX\Folder\shell\cmd_name\command -> The command to execute - arg is folder

## Submenus --- don't work!

HKXX\*\shell\top_menu
;"MUIVerb"="Top menu"
;"icon"="some.ico" or? "some.exe"
"subCommands"=""

HKXX\*\shell\top_menu\shell\sub_menu1
"MUIVerb"="Sub menu 1"

HKXX\*\shell\top_menu\shell\sub_menu1\command
@="sub_menu1.bat"

HKXX\*\shell\top_menu\shell\sub_menu2
"MUIVerb"="Sub menu 2"

HKXX\*\shell\top_menu\shell\sub_menu2\command
@="sub_menu2.bat"


Mine:
[HKEY_CURRENT_USER\Software\Classes\Directory]

[HKEY_CURRENT_USER\Software\Classes\Directory\shell]

[HKEY_CURRENT_USER\Software\Classes\Directory\shell\splunk_top_menu]
@="Top Menu"
"subcommands"=""
"MUIVerb"="TTTTT"

[HKEY_CURRENT_USER\Software\Classes\Directory\shell\splunk_top_menu\shell]

[HKEY_CURRENT_USER\Software\Classes\Directory\shell\splunk_top_menu\shell\sub_menu_1]
@="Sub Menu 1"

[HKEY_CURRENT_USER\Software\Classes\Directory\shell\splunk_top_menu\shell\sub_menu_1\command]
@="dir"

[HKEY_CURRENT_USER\Software\Classes\Directory\shell\splunk_top_menu\shell\sub_menu_2]
@="Sub Menu 2"

[HKEY_CURRENT_USER\Software\Classes\Directory\shell\splunk_top_menu\shell\sub_menu_2\command]
@="dir"




# Registry command params

https://superuser.com/questions/136838/which-special-variables-are-available-when-writing-a-shell-command-for-a-context

Important ones:
- %L – Long file name form of the first parameter. Note that Win32/64 applications will be passed the long file name, whereas Win16 applications get the short file name. Specifying %l is preferred as it avoids the need to probe for the application type.
- %D – Desktop absolute parsing name of the first parameter (for items that don't have file system paths).
- %V – For verbs that are none implies all. If there is no parameter passed this is the working directory.
- %W – The working directory.


# Splunk Commands

path = C:\Dev\repos\Apps\Splunk\Client\bin\Debug\net8.0\" "TypeSpecific" "%S" "%H" "%L" "%D" "%V" "%W"


Commander
Open a second XP in dir - aligned with first. opt for full screen?
applies to: dir
"path\SplunkClient.exe" "cmder" "%V"

Splunk\Tree
Cmd line to clipboard for current or sel dir
applies to: dir/dirbg  file?
"path\SplunkClient.exe" "tree" "%V"

Splunk\Dir
Cmd line to clipboard for current or sel dir
applies to: dir/dirbg  file?
"path\SplunkClient.exe" "dir" "%V"

Open in tab
Open dir in tab in current window
applies to: dir/desktop  (In XP use middle button)
"path\SplunkClient.exe" "newtab" "%V"

Open dir in sublime
Open dir in a new ST
applies to: dir/dirbg
"path\SplunkClient.exe" "stdir" "%V"


Maybe
find file in folder - Use everything?                        
fix nav bar + history - (re)Implement?                         
tag files/dirs - Use builtin libraries and/or favorites?
more info/hover? - filters, fullpath, size, thumbnail     

