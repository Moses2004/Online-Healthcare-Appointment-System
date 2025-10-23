using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Online_Healthcare_Appointment_System.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Extra fields common to all users
        public string? FullName { get; set; }
        public string RoleType { get; set; } // "Admin", "Doctor", "Patient"

        

    }
}
