using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dlp.Playing
{
    using System.Data.SqlClient;
    using System.Threading;

    using Dlp.Data;

    using LogPlayer.Master;

    static class StatsConsolidator
    {
        private static Queue<Stat> StatsQueue { get; set; }

        private static Task ConsolidatorTask { get; set; }

        private static bool Brake { get; set; }

        static StatsConsolidator()
        {
            StatsQueue = new Queue<Stat>();
            ConsolidatorTask = new Task(Consolidate);
            ConsolidatorTask.Start();
        }

        public static void Add(Stat stat)
        {
            StatsQueue.Enqueue(stat);
        }

        public static void Finish()
        {
            Brake = true;
            ConsolidatorTask.Wait();

            BulkInsert(StatsQueue.ToList());
        }

        private static void Consolidate()
        {
            const int BatchSize = 500;
            while(!Brake)
            {
                if (StatsQueue.Count > BatchSize)
                {
                    var stats = Dequeue(BatchSize);
                    BulkInsert(stats);
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }

        private static void BulkInsert(IEnumerable<Stat> stats)
        {
            using (var ctx = new Context())
            {
                using (var conn = new SqlConnection(ctx.Database.Connection.ConnectionString))
                {
                    conn.Open();
                    var inserter = new BulkInserter<Stat>(conn, "Stats");
                    inserter.Insert(stats);
                }
            }
        }

        private static IEnumerable<Stat> Dequeue(int count)
        {
            for (; count > 0; count--)
            {
                if (StatsQueue.Count > 0)
                {
                    yield return StatsQueue.Dequeue();
                }
            }
        }
    }
}
