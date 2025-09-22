namespace Online_Healthcare_Appointment_System.Models
{
    public class Admin
    {
        public int AdminId { get; set; }
        public string Name { get; set; }

        // Identity Link
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
