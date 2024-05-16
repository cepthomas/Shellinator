# Splunk
Games with shell extensions.

# Commands
Commander
Open a second XP in dir - aligned with first. opt for full screen?
src: Dir
"path\SplunkClient.exe" "src" "cmder" "%V"

Tree
Cmd line to clipboard for current or sel dir
src: dir/file/dirbg
"path\SplunkClient.exe" "src" "tree" "%V"

Dir
Cmd line to clipboard for current or sel dir
src: dir/file/dirbg
"path\SplunkClient.exe" "src" "dir" "%V"

Open in tab
Open dir in tab in current window
src: dir/desktop  (In XP use middle button)
"path\SplunkClient.exe" "src" "newtab" "%V"

Open dir in sublime
Open dir in a new ST
src: dir/dirbg
"path\SplunkClient.exe" "src" "stdir" "%V"


Maybe
find file in folder - Use everything?                        
fix nav bar + history - (re)Implement?                         
tag files/dirs - Use builtin libraries and/or favorites?
more info/hover? - filters, fullpath, size, thumbnail     


# Registry

HKEY_CLASSES_ROOT\Directory\shell\testcmd -> Rt Click on a selected dir
@====> Test - Directory
[HKEY_CLASSES_ROOT\Directory\shell\testcmd\command]
orig: @="cmd.exe /k \"echo %A`%B`%C`%D`%E`%F`%G`%H`%I`%J`%K`%L`%M`%N`%O`%P`%Q`%R`%S`%T`%U`%V`%W`%X`%Y`%Z\""
@="C:\Dev\repos\Apps\Splunk\Client\bin\Debug\net8.0\SplunkClient.exe" "DirShell" "%S" "%H" "%L" "%D" "%V" "%W"
args:
42:41.268 SPLCLI 19564  1      C:\Dev\repos\Apps\Splunk\Client\bin\Debug\net8.0\SplunkClient.dll
42:41.268 SPLCLI 19564  1      DirShell
42:41.269 SPLCLI 19564  1      1
42:41.269 SPLCLI 19564  1      0
42:41.269 SPLCLI 19564  1      C:\Dev\repos\Apps\Splunk\Splunk
42:41.270 SPLCLI 19564  1      C:\Dev\repos\Apps\Splunk\Splunk
42:41.270 SPLCLI 19564  1      C:\Dev\repos\Apps\Splunk\Splunk  %V
42:41.270 SPLCLI 19564  1      C:\Dev\repos\Apps\Splunk  %W


HKEY_CLASSES_ROOT\*\shell\testcmd -> Rt Click on a selected file
@====> TypeSpecific
[HKEY_CLASSES_ROOT\Directory\shell\testcmd\command]
@="C:\Dev\repos\Apps\Splunk\Client\bin\Debug\net8.0\SplunkClient.exe" "TypeSpecific" "%S" "%H" "%L" "%D" "%V" "%W"
args:
22:27.292 SPLCLI 11640  1      C:\Dev\repos\Apps\Splunk\Client\bin\Debug\net8.0\SplunkClient.dll
22:27.293 SPLCLI 11640  1      TypeSpecific
22:27.294 SPLCLI 11640  1      1
22:27.295 SPLCLI 11640  1      0
22:27.296 SPLCLI 11640  1      C:\Dev\repos\Apps\Splunk\menu1.jpg
22:27.297 SPLCLI 11640  1      C:\Dev\repos\Apps\Splunk\menu1.jpg
22:27.297 SPLCLI 11640  1      C:\Dev\repos\Apps\Splunk\menu1.jpg  %V
22:27.300 SPLCLI 11640  1      C:\Dev\repos\Apps\Splunk  %W

also:
08:40.295 SPLCLI  2976  1      C:\Dev\repos\Apps\Splunk\Client\bin\Debug\net8.0\SplunkClient.dll
08:40.296 SPLCLI  2976  1      TypeSpecific
08:40.296 SPLCLI  2976  1      1
08:40.297 SPLCLI  2976  1      0
08:40.297 SPLCLI  2976  1      C:\Users\cepth\Desktop\Clipboard_05-13-2024_01.png
08:40.298 SPLCLI  2976  1      C:\Users\cepth\Desktop\Clipboard_05-13-2024_01.png
08:40.298 SPLCLI  2976  1      C:\Users\cepth\Desktop\Clipboard_05-13-2024_01.png
08:40.299 SPLCLI  2976  1      C:\Users\cepth\Desktop


HKEY_CLASSES_ROOT\Directory\Background\shell\testcmd -> Rt Click in a dir with nothing selected
@===> Test - Directory - Background
[HKEY_CLASSES_ROOT\Directory\Background\shell\testcmd\command]
@="C:\Dev\repos\Apps\Splunk\Client\bin\Debug\net8.0\SplunkClient.exe" "DirBackgShell" "%S" "%H" "%D" "%V" "%W"
! Blows up if I use %D or %L
args:
02:59.479 SPLCLI  6736  1      C:\Dev\repos\Apps\Splunk\Client\bin\Debug\net8.0\SplunkClient.dll
02:59.490 SPLCLI  6736  1      DirBackgShell
02:59.492 SPLCLI  6736  1      1
02:59.492 SPLCLI  6736  1      0
02:59.492 SPLCLI  6736  1      C:\Dev\repos\Apps  %V
02:59.493 SPLCLI  6736  1      C:\Dev\repos\Apps  %W


> This is from desktop with no selection.
Computer\HKEY_CLASSES_ROOT\DesktopBackground\shell\testcmd

? HKEY_CLASSES_ROOT\Folder\shell\testcmd -> RtClick on dir in left pane


# Registry command params
https://superuser.com/questions/136838/which-special-variables-are-available-when-writing-a-shell-command-for-a-context
%* – Replace with all parameters.
%~ – Replace with all parameters starting with and following the second parameter.
%0 or %1 – The first file parameter. For example "C:\Users\Eric\Desktop\New Text Document.txt". Generally this should be in quotes and the applications command line parsing should accept quotes to disambiguate files with spaces in the name and different command line parameters (this is a security best practice and I believe mentioned in MSDN).
%<n> (where <n> is 2-9) – Replace with the nth parameter.
%S – Show command.
%H – Hotkey value.
%I – IDList stored in a shared memory handle is passed here.
%L – Long file name form of the first parameter. Note that Win32/64 applications will be passed the long file name, whereas Win16 applications get the short file name. Specifying %l is preferred as it avoids the need to probe for the application type.
%D – Desktop absolute parsing name of the first parameter (for items that don't have file system paths).
%V – For verbs that are none implies all. If there is no parameter passed this is the working directory.
%W – The working directory.
Also see http://www.robvanderwoude.com/ntstart.php

