using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Healthcare_Appointment_System.Data;

namespace Online_Healthcare_Appointment_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Dashboard page
        public async Task<IActionResult> Index()
        {
            ViewData["DoctorCount"] = await _context.Doctors.CountAsync();
            ViewData["PatientCount"] = await _context.Patients.CountAsync();
            ViewData["AppointmentCount"] = await _context.Appointments.CountAsync();
            ViewData["PrescriptionCount"] = await _context.Prescriptions.CountAsync();

            // Load 5 most recent appointments
            var recentAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .OrderByDescending(a => a.AppointmentDate)
                .Take(5)
                .Select(a => new
                {
                    PatientName = a.Patient.Name ?? a.Patient.Email,
                    DoctorName = a.Doctor.Name,
                    AppointmentDate = a.AppointmentDate,
                    Status = a.Status
                })
                .ToListAsync();

            ViewData["RecentAppointments"] = recentAppointments;

            return View(); // Make sure your view is /Views/AdminDashboard/Index.cshtml
        }
    }
}
