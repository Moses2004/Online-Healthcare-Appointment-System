using System;

namespace Online_Healthcare_Appointment_System.Models
{
    public class Appointment
    {
        public int AppointmentId { get; set; }  // PK
        public int PatientId { get; set; }      // FK
        public int DoctorId { get; set; }       // FK
        public DateTime AppointmentDate { get; set; }
        public string Status { get; set; }
        public string ? Notes { get; set; }

        // Navigation
        public Patient Patient { get; set; }
        public Doctor Doctor { get; set; }

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();

        public Prescription Prescription { get; set; } // one-to-one back reference
    }
}
