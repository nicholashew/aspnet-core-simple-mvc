using SimpleMvc.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleMvc.ViewModels
{
    public class TicketViewModel
    {
        public int Id { get; set; }
        public string DeviceId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string ContactNumber { get; set; }
        public int BuildingId { get; set; }
        public string LocationArea { get; set; }
        public int ProblemId { get; set; }
        public string ProblemDescription { get; set; }
        public string Remarks { get; set; }
        public int? TicketRating { get; set; }
        public TicketStatus TicketStatus { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int ModifiedBy { get; set; }
        public ICollection<AttachmentViewModel> Attachments { get; set; }
    }
}
