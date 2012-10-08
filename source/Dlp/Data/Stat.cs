using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dlp.Data
{
    using System.ComponentModel.DataAnnotations;
    using System.Net;

    class Stat
    {
        [Key]
        public int Id { get; set; }

        public string Url { get; set; }

        public long Delay { get; set; }

        public HttpStatusCode Status { get; set; }

        public bool IsException { get; set; }

        public DateTime Time { get; set; }

        public string LogEntryHash { get; set; }

        public int BatchId { get; set; }

        public int RunnerId { get; set; }

        public virtual Batch Batch { get; set; }

        public string ToCsv()
        {
            return string.Format("{0},{1},{2},{3},{4}", Url, Status, IsException, Delay, Time);
        }
    }
}
