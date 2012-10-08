using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dlp.Playing
{
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Numerics;

    using LogPlayer.Master;

    class BatchRun
    {
        private List<Task> TaskList { get; set; }

        public BatchRun(int batchId)
        {
            var range = BatchManager.GetMyRange(batchId, Configuration.RunnerId);
            TaskList = new List<Task>();
            for (int i = 0; i < Configuration.ThreadCount; i++)
            {
                var threadRange = BatchManager.GetRangeWithinRange(range.Item1, range.Item2, Configuration.ThreadCount, i);

                TaskList.Add(new Task(() => FetchAndRun(batchId, threadRange)));
            }
        }

        public void StartAndWait()
        {
            TaskList.ForEach(x => x.Start());

            Task.WaitAll(TaskList.ToArray());
        }

        private void FetchAndRun(int batchId, Tuple<BigInteger,BigInteger> range)
        {
            int minIndex = 0;
            const int MaxCount = 1000;
            List<LogEntry> entries;
            Output.BlankLine();
            Output.BlankLine();
            Output.WriteLine("Recuperando registros do banco...");
            while ((entries = BatchManager.GetEntriesInRange(batchId, range, minIndex, MaxCount)).Count > 0)
            {
                Output.WriteLine( entries.Count + " registros recuperados.");
                minIndex = entries.Last().Index + 1;
                this.MakeRequests(entries);
            }
            
        }

        private void MakeRequests(List<LogEntry> partition)
        {
            foreach (var logEntry in partition)
            {
                var retriesCounter = Configuration.Retries;
                var percent = (partition.IndexOf(logEntry) + 1) / partition.Count * 100;

                while (retriesCounter > 0)
                {
                    var watch = Stopwatch.StartNew();

                    try
                    {
                        var url = logEntry.Uri;
                        var req = WebRequest.CreateHttp(url);
                        req.Method = logEntry.Method;

                        var resp = (HttpWebResponse)req.GetResponse();
                        ReadAndDump(resp);

                        watch.Stop();

                        retriesCounter = 0;
                        Output.Write(string.Format("[{1}%] Chamando \"{0}\"... ", url, percent), false);
                        Output.WriteLine(string.Format("({0}) {1}ms", resp.StatusCode, watch.ElapsedMilliseconds), false);

                        StatsCollector.Success(logEntry, resp.StatusCode, watch.ElapsedMilliseconds);

                        resp.Close();
                        break;
                    }
                    catch (Exception ex)
                    {
                        watch.Stop();

                        var webex = ex as WebException;
                        if (webex != null)
                        {
                            var resp = webex.Response as HttpWebResponse;
                            if (resp != null)
                            {
                                StatsCollector.Success(logEntry, resp.StatusCode, watch.ElapsedMilliseconds);
                                break;
                            }
                        }

                        retriesCounter--;
                        Output.Write(string.Format("[{1}%] Chamando \"{0}\"... ", logEntry.Uri, percent), false);
                        Output.WriteLine(retriesCounter > 0 ? "Erro. Tentando novamente..." : "Erro. Desistindo...", false);

                        StatsCollector.Exception(logEntry);
                    }
                }
            }
        }

        private void ReadAndDump(HttpWebResponse resp)
        {
            var stream = resp.GetResponseStream();
            if (stream != null)
            {
                var rdr = new StreamReader(stream);

                int buflen = 100000;
                int offset = 0;

                var buffer = new char[buflen];

                while (rdr.Read(buffer, offset, buflen)>0) ;
            }
        }
    }
}
