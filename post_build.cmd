
echo off

rem After building, copy client products to where server can find them.

copy Client\bin\Debug\net8.0\SplunkClient.dll Splunk\bin\Debug\net8.0-windows
copy Client\bin\Debug\net8.0\SplunkClient.exe Splunk\bin\Debug\net8.0-windows
copy Client\bin\Debug\net8.0\SplunkClient.runtimeconfig.json Splunk\bin\Debug\net8.0-windows
