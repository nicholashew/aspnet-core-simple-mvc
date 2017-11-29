using SimpleMvc.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleMvc.Models
{

    public class Ticket
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        [StringLength(256)]
        public string Email { get; set; }

        [Required]
        [StringLength(50)]
        public string ContactNumber { get; set; }

        public int BuildingId { get; set; }
        public int ProblemId { get; set; }
        public string LocationArea { get; set; }
        public string ProblemDescription { get; set; }
        public string Remarks { get; set; }

        [Range(0, 5)]
        public int? TicketRating { get; set; }
        public TicketStatus TicketStatus { get; set; }
        public DateTime? TicketCompletedDate { get; set; }        

        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int ModifiedBy { get; set; }

        public virtual Problem Problem { get; set; }
        public virtual Building Building { get; set; }
        public ICollection<Attachment> Attachments { get; set; }
    }
}