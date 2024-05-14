using System;
using Ipc = Ephemera.NBagOfTricks.SimpleIpc;



namespace Splunk.Client
{
    internal class Program
    {
        // TODO1 copied - or config?
        const string PIPE_NAME = "058F684D-AF82-4FE5-BD1E-9FD031FE28CF";
        const string LOGFILE = @"C:\Dev\repos\Splunk\test_ipc_log.txt";
        //static readonly Ipc.MpLog _log = new(LOGFILE, "CLI");

        static Ipc.Client client;// = new(PIPE_NAME, LOGFILE);

        static void Main(string[] _)
        {
            var cl = Environment.CommandLine;

            Console.WriteLine($"Client says [{cl}]");

            foreach (var arg in Environment.GetCommandLineArgs())
            {
                Console.WriteLine($"  {arg}");
            }

            try
            {
                client = new(PIPE_NAME, LOGFILE);
                var res = client.Send($"{cl}", 1000);

                switch (res)
                {
                    case Ipc.ClientStatus.Ok:
                        Console.WriteLine($"Client ok");
                        break;

                    case Ipc.ClientStatus.Error:
                        Console.WriteLine($"Client error:{client.Error}", true);
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
        }

        //static void Main(string[] _)
        //{
        //    var cl = Environment.CommandLine;

        //    Ipc.Client client = new(PIPE_NAME, LOGFILE);
        //    _log.Write($"Client says [{cl}]");

        //    foreach (var arg in Environment.GetCommandLineArgs())
        //    {
        //        _log.Write($"  {arg}");
        //    }

        //    var res = client.Send($"{cl}", 1000);

        //    switch (res)
        //    {
        //        case Ipc.ClientStatus.Ok:
        //            _log.Write($"Client ok");
        //            break;

        //        case Ipc.ClientStatus.Error:
        //            _log.Write($"Client error:{client.Error}", true);
        //            break;

        //        case Ipc.ClientStatus.Timeout:
        //            _log.Write($"Client timeout", true);
        //            break;
        //    }
        //}
    }
}
