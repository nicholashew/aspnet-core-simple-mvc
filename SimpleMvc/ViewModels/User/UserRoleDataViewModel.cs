using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleMvc.ViewModels.User
{
    public class UserRoleDataViewModel
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="UserRoleDataViewModel"/> is check.
        /// </summary>
        /// <value><c>true</c> if check; otherwise, <c>false</c>.</value>
        public bool Check { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="UserRoleDataViewModel"/> is disabled.
        /// </summary>
        /// <value><c>true</c> if disabled; otherwise, <c>false</c>.</value>
        public bool Disabled { get; set; }
    }
}
