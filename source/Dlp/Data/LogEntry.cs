using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogPlayer.Master
{
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;
    using System.Security.Cryptography;
    using System.Text.RegularExpressions;

    public class LogEntry
    {
        public LogEntry()
        {
            
        }

        public LogEntry(Match match, int i)
        {
            var query = "?" + GroupOrDefault(match, "query", string.Empty, "-");
            
            Path = match.Groups["path"].Value + query.TrimEnd('?');
            Host = GroupOrDefault(match, "host", Configuration.HostOverride);
            Method = GroupOrDefault(match, "method", "GET");
            Protocol = GroupOrDefault(match, "protocol", "http");
            Date = GroupOrDefault(match, "datetime", new DateTime(2000, 1, 1));
            Index = i;
            
            if (match.Groups["port"].Success && match.Groups["port"].Value == "443")
            {
                Protocol = "https";
            }

            Hash = MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(i + Method + Uri)).ToHexString();
        }

        [Key]
        public string Hash { get; private set; }

        public int BatchId { get; set; }

        public string Method { get; private set; }

        public string Host { get; private set; }

        public string Protocol { get; private set; }

        public string Path { get; private set; }

        public int Index { get; private set; }

        public DateTime Date { get; private set; }

        public string Uri
        {
            get
            {
                return Protocol + "://" + Host + Path;
            }
        }

        private static string GroupOrDefault(Match match, string groupname, string defValue = null, string nullChar = null)
        {
            var result = match.Groups[groupname].Success ? match.Groups[groupname].Value : defValue;
            
            return result == nullChar ? defValue : result;
        }

        private DateTime GroupOrDefault(Match match, string groupname, DateTime defValue)
        {
            var result = match.Groups[groupname].Success ? match.Groups[groupname].Value : string.Empty;

            DateTime date;
            return DateTime.TryParse(result, out date) ? date : defValue;
        }
    }
}
