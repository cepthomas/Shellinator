using System;
using System.IO;
using Ipc = Ephemera.NBagOfTricks.SimpleIpc;
using Com = Splunk.Common.Common;
using System.Collections.Generic;
using Ephemera.NBagOfTricks;


namespace Splunk.Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //Ipc.MpLog _log = new(Com.LogFileName, "SPLCLI");
            //_log.Write($"Client cmd line: [{Environment.CommandLine}]");
            //Console.WriteLine($"Client cmd line: [{Environment.CommandLine}]");

            // Clean up args and make them safe for server by quoting.
            List<string> cleanArgs = [];
            args.ForEach(a => { cleanArgs.Add($"\"{a}\""); });
            var cmdString = string.Join(" ", cleanArgs);
            Console.WriteLine($"Send: {cmdString}");

            try
            {
                Ipc.Client ipcClient = new(Com.PIPE_NAME, Com.LogFileName);
                var res = ipcClient.Send(cmdString, 1000);
                var error = res == Ipc.ClientStatus.Error;
                Console.WriteLine($"Result: {res} {(error ? ipcClient.Error : "")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client failed: {ex.Message}");
            }

            System.Threading.Thread.Sleep(2000);
        }
    }
}
