using System.ComponentModel.DataAnnotations;

namespace Online_Healthcare_Appointment_System.Models
{
    public class DoctorRegisterViewModel
    {
        [Required]
        public string FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        [Required, DataType(DataType.PhoneNumber)]
        public string PhoneNumber { get; set; }

        public bool Availability { get; set; }

        [Required]
        public int SpecializationId { get; set; }

        [Required]
        public decimal ConsultationFee { get; set; }
    }
}
