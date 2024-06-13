
echo off
cls

dir

rem This opens a new window. Avoid the trailing \" !!
rem explorer.exe /select,"C:\Dev\SplunkStuff\test_dir\"

rem This works (w/flash)
rem tree /a /f "C:\Dev\SplunkStuff\test_dir" > _cmd_out.txt
rem cmd /c /q tree /a /f "C:\Dev\SplunkStuff\test_dir" | clip

rem C:\>start /?
rem Starts a separate window to run a specified program or command.
rem START ["title"] [/D path] [/I] [/MIN] [/MAX] [/SEPARATE | /SHARED]
rem   [/LOW | /NORMAL | /HIGH | /REALTIME | /ABOVENORMAL | /BELOWNORMAL]
rem   [/NODE <NUMA node>] [/AFFINITY <hex affinity mask>] [/WAIT] [/B]
rem   [command/program] [parameters]
rem 
rem "title"     Title to display in window title bar.
rem path        Starting directory.
rem B           Start application without creating a new window. The
rem             application has ^C handling ignored. Unless the application
rem             enables ^C processing, ^Break is the only way to interrupt
rem             the application.
