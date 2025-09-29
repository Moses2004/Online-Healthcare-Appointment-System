using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Online_Healthcare_Appointment_System.Data;

namespace Online_Healthcare_Appointment_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            var doctorCount = _context.Doctors.Count();
            var patientCount = _context.Patients.Count();
            var appointmentCount = _context.Appointments.Count();

            ViewData["DoctorCount"] = doctorCount;
            ViewData["PatientCount"] = patientCount;
            ViewData["AppointmentCount"] = appointmentCount;

            return View();
        }
    }
}
