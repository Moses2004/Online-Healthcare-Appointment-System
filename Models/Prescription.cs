using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations;

namespace Online_Healthcare_Appointment_System.Models
{
    public class Prescription
    {
        public int PrescriptionId { get; set; }   // PK

        [Required]
        public int AppointmentId { get; set; }    // FK to Appointment

        [StringLength(2000)]
        public string DoctorNotes { get; set; }

        [Required, StringLength(4000)]
        public string PrescriptionDetails { get; set; }

        public DateTime DateIssued { get; set; } = DateTime.UtcNow;

        // Navigation
        [ValidateNever]
        public Appointment Appointment { get; set; }
    }
}
