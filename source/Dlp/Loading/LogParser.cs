using System.Collections.Generic;
using System.Linq;

namespace Dlp.Loading
{
    using System;
    using System.IO;
    using System.Text.RegularExpressions;

    using LogPlayer.Master;

    static class LogParser
    {
        public static void Load(string logFilePath, string pattern, List<string> filterPatterns, int batchId, Action<List<LogEntry>> write)
        {
            Output.WriteLine("Lendo arquivo de log: " + logFilePath);
            var r = new Regex(pattern);
            var filters = filterPatterns.Select(x => new Regex(x));
            
            var rdr = new StreamReader(File.OpenRead(logFilePath));

            List<string> lines;
            int[] counter = { 0 };
            while ((lines = rdr.ReadLines(10000).ToList()).Any())
            {
                Output.WriteLine(lines.Count + " linhas lidas.");

                Output.BlankLine();
                Output.Write("Filtrando... ");
                lines = lines.Where(x => !filters.Any(filter => filter.IsMatch(x))).ToList();
                Output.WriteLine(lines.Count + " linhas após filtragem.");

                Output.BlankLine();
                Output.Write("Aplicando pattern... ");
                var matches = lines
                    .Select(x => r.Match(x))
                    .OfType<Match>()
                    .Where(x => x.Success).ToList();
                Output.WriteLine(matches.Count() + " resultados encontrados.");

                Output.BlankLine();
                Output.Write("Convertendo para LogEntry... ");

                var newItems = matches
                    .Where(x => x.Groups["path"].Success && !string.IsNullOrEmpty(x.Groups["path"].Value))
                    .Select
                        (x => new LogEntry(x, counter[0]++) { BatchId = batchId })
                    .ToList();
                Output.WriteLine("OK.");

                write(newItems);
            }
        }

        private static IEnumerable<string> ReadLines(this StreamReader rdr, int count)
        {
            for (; count > 0 && !rdr.EndOfStream; count--)
            {
                yield return rdr.ReadLine();
            }
        }
    }
}
