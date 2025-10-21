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

        //  Dashboard page for Admin
        public async Task<IActionResult> Index()
        {
            // Summary counts
            ViewData["DoctorCount"] = await _context.Doctors.CountAsync();
            ViewData["PatientCount"] = await _context.Patients.CountAsync();
            ViewData["AppointmentCount"] = await _context.Appointments.CountAsync();
            ViewData["PrescriptionCount"] = await _context.Prescriptions.CountAsync();

            //  Directly return the dashboard view (no need for recent appointments now)
            return View("~/Views/Admin/DashBoard.cshtml");
        }
    }
}
