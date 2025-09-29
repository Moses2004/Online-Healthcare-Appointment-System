namespace Online_Healthcare_Appointment_System.Models
{
    public class Doctor
    {
        public int DoctorId { get; set; }
        public string Name { get; set; }
        public int SpecializationId { get; set; }
        public bool Availability { get; set; }
        public decimal ConsultationFee { get; set; }

        // Identity Link
        public string UserId { get; set; }  // FK to AspNetUsers
        public ApplicationUser User { get; set; }

        // Navigation
        public Specialization Specialization { get; set; }

        public bool IsApproved { get; set; } = false;

    }
}
