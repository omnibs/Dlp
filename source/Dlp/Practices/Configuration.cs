using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogPlayer.Master
{
    using System.IO;
    using System.Text.RegularExpressions;

    class Configuration
    {
        private static readonly Regex ConfigLineRegex = new Regex("(?<type>match|filter|threads|host) (?<arg>.+)");
        
        public static void Load()
        {
            var configPath = Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).FullName + @"\config.txt";
            var lines = File.ReadAllLines(configPath).ToList();

            var configEntries = lines
                .Where(x => !x.Trim().StartsWith("#"))
                .Select(x => ConfigLineRegex.Match(x.Trim()))
                .OfType<Match>()
                .Where(x => x.Groups["type"].Success && x.Groups["arg"].Success)
                .Select(x => new { Type = x.Groups["type"].Value, Arg = x.Groups["arg"].Value})
                .ToList();

            LogEntryPattern = configEntries.Last(x => x.Type == "match").Arg;

            FilterPatterns = configEntries.Where(x => x.Type == "filter").Select(x => x.Arg).ToList();

            var threadConfig = configEntries.LastOrDefault(x => x.Type == "threads");
            int threadCount;
            ThreadCount = threadConfig != null && int.TryParse(threadConfig.Arg, out threadCount) ? threadCount : 5;

            var hostConfig = configEntries.LastOrDefault(x => x.Type == "host");
            HostOverride = hostConfig != null ? hostConfig.Arg : null;

            var retriesConfig = configEntries.LastOrDefault(x => x.Type == "retries");
            int retries;
            Retries = retriesConfig!= null && int.TryParse(retriesConfig.Arg, out retries) ? retries : 5;
        }

        public static string LogEntryPattern { get; private set; }

        public static List<string> FilterPatterns { get; private set; }

        public static string ServerAddress { get; set; }

        public static int ThreadCount { get; private set; }

        /// <summary>
        /// Host a ser usado nos webrequests
        /// </summary>
        public static string HostOverride { get; private set; }

        public static int Retries { get; private set; }

        public static int RunnerId { get; set; }
    }
}
