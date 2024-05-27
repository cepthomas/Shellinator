using System;
using System.IO;
using System.Collections.Generic;
using Ephemera.NBagOfTricks;


namespace Splunk.Client
{
    /// <summary>The worker. Public for testing ease.</summary>
    public class Client
    {
        public void Run(string[] args, TextWriter twr)
        {
            try
            {
                // Clean up args and make them safe for server by quoting before concatenating.
                List<string> cleanArgs = [];
                
                // Fix corner case for accidental escaped quote.
                args.ForEach(a => { cleanArgs.Add($"\"{a.Replace("\"", "")}\""); });
                var cmdString = string.Join(" ", cleanArgs);
                //twr.WriteLine($"Send: {cmdString}");

                Ephemera.NBagOfTricks.SimpleIpc.Client ipcClient = new(Common.Common.PIPE_NAME, Common.Common.LogFileName);
                var res = ipcClient.Send(cmdString, 1000);
                if (res != Ephemera.NBagOfTricks.SimpleIpc.ClientStatus.Ok)
                {
                    twr.WriteLine($"Result: {res} {ipcClient.Error}");
                }
            }
            catch (Exception ex)
            {
                twr.WriteLine($"Client failed: {ex.Message}");
            }
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            new Client().Run(args, Console.Out);
        }
    }
}
