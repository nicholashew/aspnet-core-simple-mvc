using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleMvc.Models
{
    public class Attachment
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string FileName { get; set; }
        public string FileBinary { get; set; }

        public Ticket Ticket { get; set; }
    }
}
