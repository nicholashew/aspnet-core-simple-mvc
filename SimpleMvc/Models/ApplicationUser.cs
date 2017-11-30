using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SimpleMvc.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser<int>
    {
        public ApplicationUser()
            : base()
        {
        }

        public ApplicationUser(string userName)
            : base(userName)
        {
        }

        // Additional profile data
        [StringLength(50, ErrorMessage = "First name cannot be longer than 50 characters.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Birth Date")]
        public DateTime? BirthDate { set; get; }

        [StringLength(100)]
        public string Address1 { get; set; }

        [StringLength(100)]
        public string Address2 { get; set; }

        [StringLength(100)]
        public string State { get; set; }

        [StringLength(100)]
        public string City { get; set; }

        [StringLength(50)]
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; }

        [StringLength(100)]
        public string Country { get; set; }

        /// <summary>
        /// Property used for soft deleting users
        /// </summary>
        [Display(Name = "Deleted")]
        public bool IsDeleted { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        [Display(Name = "Last Deleted Date")]
        public DateTime? LastDeletedDate { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        [Display(Name = "Last Restored Date")]
        public DateTime? LastRestoredDate { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        public int? CreatedBy { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        [Display(Name = "Modified Date")]
        public DateTime? ModifiedDate { get; set; }

        public int? ModifiedBy { get; set; }

        [Display(Name = "Full Name")]
        public string FullName
        {
            get
            {
                return LastName + ", " + FirstName;
            }
        }

        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        [Display(Name = "Last Modified Date")]
        public DateTime LastModifiedDate
        {
            get
            {
                return ModifiedDate.GetValueOrDefault() > new DateTime(0) ? ModifiedDate.GetValueOrDefault() : CreatedDate;
            }
        }
    }
}
