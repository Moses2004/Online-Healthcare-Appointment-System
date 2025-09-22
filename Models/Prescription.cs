namespace Online_Healthcare_Appointment_System.Models
{
    public class Prescription
    {
        public int PrescriptionId { get; set; }  // PK
        public int AppointmentId { get; set; }  // FK
        public string DoctorNotes { get; set; }
        public string PrescriptionDetails { get; set; }
        public DateTime DateIssued { get; set; }

        // Navigation
        public Appointment Appointment { get; set; }
    }
}
