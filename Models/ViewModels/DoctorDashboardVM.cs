using System;
using System.Collections.Generic;

namespace Online_Healthcare_Appointment_System.Models.ViewModels
{
    public class AppointmentInfo
    {
        public DateTime AppointmentDate { get; set; }
        public string PatientName { get; set; }
        public string Status { get; set; }
    }

    public class DoctorDashboardVM
    {
        public string DoctorName { get; set; }
        public List<AppointmentInfo> UpcomingAppointments { get; set; } = new();

        public List<AppointmentInfo> PreviousAppointments { get; set; } = new();
        public int TodayCount { get; set; }
        public int ThisWeekCount { get; set; }

        public List<Feedback> RecentFeedbacks { get; set; } = new();

    }
}
