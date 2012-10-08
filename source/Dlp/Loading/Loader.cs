using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dlp.Loading
{
    using System.Data.SqlClient;

    using Dlp.Data;

    using LogPlayer.Master;

    class Loader
    {
        public static void LoadAndWrite(string logFile)
        {
            var batch = new Batch() { CreatedDate = DateTime.Now };
            using (var ctx = new Context())
            {
                ctx.Batches.Add(batch);
                ctx.SaveChanges();
            }
            
            LogParser.Load(logFile, Configuration.LogEntryPattern, Configuration.FilterPatterns, batch.Id, Write);

            Output.BlankLine();
            Output.WriteLine("BatchId=" + batch.Id);
        }

        public static void Write(List<LogEntry> logs)
        {
            using (var ctx = new Context())
            {
                Output.BlankLine();
                Output.Write("Escrevendo registros no banco... ");
                var tableName = "LogEntries";
                var bufferSize = 5000;
                using (var conn = new SqlConnection(ctx.Database.Connection.ConnectionString))
                {
                    conn.Open();
                    var inserter = new BulkInserter<LogEntry>(conn, tableName, bufferSize);
                    inserter.PostBulkInsert += (sender, args) => Output.Write((logs.Count - args.Items.Count()) + " itens restantes... ");
                    inserter.Insert(logs);
                }
                Output.WriteLine(" concluído.");
            }
        }
    }
}
