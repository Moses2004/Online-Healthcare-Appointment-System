using System;

namespace Online_Healthcare_Appointment_System.Models
{
    public class Patient
    {
        public int PatientId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }

        public string Phone { get; set; }
        public DateTime DOB { get; set; }
        public string Gender { get; set; }
        public string Address { get; set; }

        // Identity Link
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
