using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dlp
{
    using System.Data.SqlClient;

    using Dlp.Data;
    using Dlp.Loading;
    using Dlp.Playing;

    using LogPlayer.Master;

    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            // args = new []{"load", "partial.log"};
            // args = new[] { "register", "1" };
            args = new[] { "play", "1", "2"};
#endif
            Configuration.Load();

            if (args.Length < 2)
            {
                UsoIncorreto();
                return;
            }

            switch (args[0])
            {
                case "load":
                    if (args.Length != 2)
                    {
                        UsoIncorreto();
                        return;
                    }
                    Loader.LoadAndWrite(args[1]);
                    break;
                case "register":
                    int batchId;
                    if (!int.TryParse(args[1], out batchId))
                    {
                        Output.WriteLine("Batch id não é um número inteiro válido");
                        return;
                    }
                    var runnerId = BatchManager.Register(batchId);
                    Output.WriteLine("Registrado com sucesso.");
                    Output.WriteLine("Seu runnerId é " + runnerId);

                    Console.ReadKey();
                    break;
                case "play":
                    int bid;
                    if (!int.TryParse(args[1], out bid))
                    {
                        Output.WriteLine("Batch id não é um número inteiro válido");
                        return;
                    }
                    int rid;
                    if (!int.TryParse(args[2], out rid))
                    {
                        Output.WriteLine("Batch id não é um número inteiro válido");
                        return;
                    }

                    Configuration.RunnerId = rid;
                    var run = new BatchRun(bid);
                    run.StartAndWait();

                    StatsCollector.Persist();
                    Console.ReadKey();
                    break;
                default:
                    UsoIncorreto();
                    return;
            }
        }

        private static void UsoIncorreto()
        {
            Output.WriteLine("Uso incorreto");
            Output.WriteLine("dlp load [logfile.log]");
            Output.WriteLine("dlp load [logfile.log]");
            Output.WriteLine("dlp register [batch number]");
            Output.WriteLine("dlp play [batch number] [runner id]");
        }
    }
}
