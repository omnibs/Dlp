using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogPlayer.Master
{
    using System.Data.SqlClient;
    using System.IO;
    using System.Net;
    using System.Runtime.Serialization;

    using Dlp;
    using Dlp.Data;
    using Dlp.Playing;

    static class StatsCollector
    {
        public static List<Stat> Stats { get; set; }

        static StatsCollector()
        {
            Stats = new List<Stat>();
        }

        public static void Exception(LogEntry entry)
        {
            var item = new Stat() { IsException = true, Url = entry.Uri, Time = DateTime.Now, Status = HttpStatusCode.Unused, LogEntryHash = entry.Hash, BatchId = entry.BatchId, RunnerId = Configuration.RunnerId};
            Stats.Add(item);
            StatsConsolidator.Add(item);
        }

        public static void Success(LogEntry entry, HttpStatusCode statusCode, long elapsedMilliseconds)
        {
            var item = new Stat()
                {
                    Delay = elapsedMilliseconds,
                    IsException = false,
                    Status = statusCode,
                    Url = entry.Uri,
                    Time = DateTime.Now,
                    LogEntryHash = entry.Hash,
                    BatchId = entry.BatchId,
                    RunnerId = Configuration.RunnerId
                };
            Stats.Add(item);
            StatsConsolidator.Add(item);

            //switch (resp.StatusCode)
            //{
            //    case HttpStatusCode.NotFound:
            //    case HttpStatusCode.Forbidden:
            //    case HttpStatusCode.Unauthorized:
            //    case HttpStatusCode.BadRequest:
            //    case HttpStatusCode.ProxyAuthenticationRequired:
            //    case HttpStatusCode.NoContent:
            //    case HttpStatusCode.MethodNotAllowed:
            //    case HttpStatusCode.InternalServerError:
                    
            //        break;
            //    case HttpStatusCode.Redirect:
            //    case HttpStatusCode.Moved:
                    
            //        break;
            //    case HttpStatusCode.OK:
                    
            //        break;
            //    default:
                    
            //}
        }

        public static void Persist()
        {
            //using (var ctx = new Context())
            //{
            //    var logs = Stats;

            //    Output.BlankLine();
            //    Output.Write("Escrevendo resultados no banco... ");
            //    var tableName = "Stats";
            //    var bufferSize = 5000;
            //    using (var conn = new SqlConnection(ctx.Database.Connection.ConnectionString))
            //    {
            //        conn.Open();
            //        var inserter = new BulkInserter<Stat>(conn, tableName, bufferSize);
            //        inserter.PostBulkInsert += (sender, args) => Output.Write((logs.Count - args.Items.Count()) + " itens restantes... ");
            //        inserter.Insert(logs);
            //    }
            //    Output.WriteLine(" concluído.");
            //}

            StatsConsolidator.Finish();

            StatisticsDump(Stats);
            CsvDump(Stats);
        }

        private static void StatisticsDump(List<Stat> stats)
        {
            var success = stats.Where(x => !x.IsException).ToList();
            var statistics = string.Empty;

            if (success.Count == 0)
            {
                statistics = "Todos os requests deram erro/timeout.";
            }
            else
            {
                var exceptions = stats.Where(x => x.IsException).Count();

                var average = success.Average(x => x.Delay);
                var min = success.Min(x => x.Delay);
                var max = success.Max(x => x.Delay);

                var byUrl = success.GroupBy(x => x.Url, y => y).ToList();

                var bySpeed = byUrl.Select(x => new { Url = x.Key, Value = x.Average(y => y.Delay) }).OrderBy(x => x.Value).ToList();

                var topFastest = bySpeed.Take(10);
                var topSlowest = bySpeed.Skip(bySpeed.Count - 10);

                var byCount = byUrl.Select(x => new { Url = x.Key, Value = x.Count() }).OrderByDescending(x => x.Value).Take(10);

                statistics = string.Format(
@"Min={0}ms; Med={1}ms; Max={2}ms;

Top 10 mais rapidos:
{3}

Top 10 mais lentos:
{4}

Top 10 mais requests:
{5}

Exceptions: {6}", min, average, max, topFastest.Format(), topSlowest.Format(), byCount.Format(), exceptions);
            }

            File.WriteAllText("stats.txt", statistics);
        }

        private static void CsvDump(List<Stat> stats)
        {
            var csvs = stats.Select(x => x.ToCsv());
            var text = string.Join(Environment.NewLine, csvs);
            File.WriteAllText("reqs.csv", text);
        }

        private static string Format(this IEnumerable<dynamic> items)
        {
            return string.Join(Environment.NewLine, items.Select(x => x.Value + "\t\t" + x.Url));
        }
    }
}
