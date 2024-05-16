using System;
using System.IO;
using Ipc = Ephemera.NBagOfTricks.SimpleIpc;
using Com = Splunk.Common.Common;


namespace Splunk.Client
{
    internal class Program
    {
        static Ipc.MpLog _log;

        static Ipc.Client _client;

        static void Main(string[] _)
        {
            _log = new(Com.LogFileName, "SPLCLI");

            var cl = Environment.CommandLine;
            _log.Write($"Client says [{cl}]");

            Console.WriteLine($"Client says [{cl}]");

            foreach (var arg in Environment.GetCommandLineArgs())
            {
                _log.Write($"    {arg}");
                Console.WriteLine($"  {arg}");
            }

            try
            {
                _client = new(Com.PipeName, Com.LogFileName);

                var res = _client.Send($"{cl}", 1000);

                _log.Write($"res:{res}");

                switch (res)
                {
                    case Ipc.ClientStatus.Ok:
                        Console.WriteLine($"Client ok");
                        break;

                    case Ipc.ClientStatus.Error:
                        Console.WriteLine($"Client error:{_client.Error}", true);
                        break;

                    case Ipc.ClientStatus.Timeout:
                        Console.WriteLine($"Client timeout", true);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            System.Threading.Thread.Sleep(2000);
        }
    }
}
