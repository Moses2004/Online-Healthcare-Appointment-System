using System;
using System.Collections.Generic;

namespace Online_Healthcare_Appointment_System.Models.ViewModels
{
    public class PatientAppointmentInfo
    {
        public DateTime AppointmentDate { get; set; }
        public string DoctorName { get; set; }
        public string Status { get; set; }
    }

    public class PatientDashboardVM
    {
        public string PatientName { get; set; }

        // New properties
        public int TodayCount { get; set; }
        public int ThisWeekCount { get; set; }
        public List<PatientAppointmentInfo> UpcomingAppointments { get; set; }
    }
}
