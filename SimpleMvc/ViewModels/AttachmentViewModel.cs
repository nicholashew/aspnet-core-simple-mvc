using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleMvc.ViewModels
{
    public class AttachmentViewModel
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string FileName { get; set; }
        public string FileBinary { get; set; }
    }
}
