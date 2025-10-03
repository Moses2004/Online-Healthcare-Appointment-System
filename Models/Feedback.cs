namespace Online_Healthcare_Appointment_System.Models
{
    public class Feedback
    {
        public int FeedbackId { get; set; }  // PK
        public int PatientId { get; set; }   // FK
        public int DoctorId { get; set; }    // FK
        public string Comments { get; set; }
        public int Rating { get; set; }
        public DateTime DateSubmitted { get; set; }

        // Navigation
        public Patient Patient { get; set; }
        public Doctor Doctor { get; set; }

        

    }
}
