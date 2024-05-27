
echo off
cls


rem https://stackoverflow.com/questions/1073353/c-how-to-open-windows-explorer-windows-with-a-number-of-files-selected

rem This opens a new window
rem explorer.exe /select,"C:\Dev\SplunkStuff\test_dir\"
rem
rem /e   
rem /idlist,:handle:process
rem     specifies object as ITEMIDLIST in shared memory block with given handle in context of given process
rem
rem /n
rem     redundant in Windows Vista
rem 
rem /root,/idlist,:handle:process
rem /root,clsid
rem /root,clsid,path
rem /root,path
rem     specifies object as root
rem
rem /select
rem     show object as selected item in parent folder
rem
rem /separate
rem     show in separate EXPLORER process
rem
rem path
rem     specifies object;
rem     ignored if object already specified;
rem     overridden by specification in later /idlist or /root argument




rem "C:\Dev\repos\Apps\Splunk\Client\bin\Debug\net8.0\SplunkClient.exe" "cmder" "yyy"

rem "C:\Dev\repos\Apps\Splunk\Client\bin\Debug\net8.0\SplunkClient.exe" "tree" "C:\Dev\SplunkStuff\test_dir\"

rem "C:\Dev\repos\Apps\Splunk\Client\bin\Debug\net8.0\SplunkClient.exe" "newtab" "yyy"

rem "C:\Dev\repos\Apps\Splunk\Client\bin\Debug\net8.0\SplunkClient.exe" "stdir" "C:\Dev\SplunkStuff\test_dir"

rem "C:\Dev\repos\Apps\Splunk\Client\bin\Debug\net8.0\SplunkClient.exe" "find" "C:\Dev\SplunkStuff\test_dir"
rem "C:\Program Files\Everything\everything" -parent "C:\Dev\SplunkStuff\test_dir"

rem "C:\Dev\repos\Apps\Splunk\Client\bin\Debug\net8.0\SplunkClient.exe" "baaaad" "yyy"

rem "C:\Dev\repos\Apps\Splunk\Client\bin\Debug\net8.0\SplunkClient.exe" "A1 B2" C3


