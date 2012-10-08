using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dlp.Data
{
    using LogPlayer.Master;

    class Batch
    {
        public int Id { get; set; }

        public DateTime CreatedDate { get; set; }

        public virtual List<LogEntry> Logs { get; set; }
    }
}
