using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dlp.Playing
{
    using System.Numerics;
    using System.Transactions;

    using Dlp.Data;

    using LogPlayer.Master;

    class BatchManager
    {
        public static int Register(int batchId)
        {
            int runnerId;
            using (var ctx = new Context())
            {
                //using (var scope = new TransactionScope(TransactionScopeOption.Required))
                //{
                    runnerId =
                        ctx.BatchRunners.Where(x => x.BatchId == batchId).OrderByDescending(x => x.RunnerId).Select(
                            x => x.RunnerId).FirstOrDefault() + 1;
                    ctx.BatchRunners.Add(new BatchRunner() { BatchId = batchId, RunnerId = runnerId });
                    ctx.SaveChanges();
                //    scope.Complete();
                //}
            }
            return runnerId;
        }

        public static List<LogEntry> GetEntriesInRange(int batchId, Tuple<BigInteger,BigInteger> range, int minIndex = 0, int maxCount = int.MaxValue)
        {
            using (var ctx = new Context())
            {
                var min = range.Item1.ToString("X32");
                var max = range.Item2.ToString("X32");
                min = min.Substring(min.Length - 32, 32);
                max = max.Substring(max.Length - 32, 32);

                return ctx.Logs.SqlQuery(
                    string.Format(
                        "select top {3} * from LogEntries where BatchId = {0} and Hash between '{1}' and '{2}' order by Hash", batchId, min, max, maxCount)).ToList();
            }
        }

        public static Tuple<BigInteger, BigInteger> GetMyRange(int batchId, int runnerId)
        {
            Tuple<BigInteger, BigInteger> result;
            using (var ctx = new Context())
            {
                var count = ctx.BatchRunners.Count(x => x.BatchId == batchId);
                runnerId = runnerId - 1;
                
                var max = BigInteger.Parse("340282366920938463463374607431768211455");
                
                result = GetRangeWithinRange(BigInteger.Parse("0"), max, count, runnerId);
            }

            return result;
        }

        /// <summary>
        /// Calcula um range thisPart dentro de outro range maior (start-end) dividido em parts partes
        /// </summary>
        /// <param name="start">Inicio do range maior</param>
        /// <param name="end">Fim do range maior</param>
        /// <param name="parts">Em quantas partes o range maior será dividido</param>
        /// <param name="thisPart">Qual parte queremos</param>
        /// <returns>Tupla com início e fim do range menor</returns>
        public static Tuple<BigInteger, BigInteger> GetRangeWithinRange(BigInteger start, BigInteger end, int parts, int thisPart)
        {
            var space = (end - start) / parts;
            var first = space * thisPart;
            var nextPartFirst = space * (thisPart + 1);
            var last = thisPart == parts - 1 ? end :  nextPartFirst - 1;
            
            return new Tuple<BigInteger, BigInteger>(first, last);
        }
    }
}
