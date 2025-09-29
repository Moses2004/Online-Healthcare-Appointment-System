using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Healthcare_Appointment_System.Data;
using Online_Healthcare_Appointment_System.Models;
using Online_Healthcare_Appointment_System.Models.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Online_Healthcare_Appointment_System.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DoctorDashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: DoctorDashboard
        public async Task<IActionResult> Index()
        {
            // Get current logged-in user
            var user = await _userManager.GetUserAsync(User);

            // Match this user with a Doctor record
            var doctor = _context.Doctors.FirstOrDefault(d => d.UserId == user.Id);

            var vm = new DoctorDashboardVM
            {
                DoctorName = doctor?.Name ?? user.FullName
            };

            if (doctor != null)
            {
                // Get today's and week's range
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                var endOfWeek = startOfWeek.AddDays(7);

                // Count today's appointments
                vm.TodayCount = _context.Appointments
                    .Count(a => a.DoctorId == doctor.DoctorId &&
                                a.AppointmentDate >= today &&
                                a.AppointmentDate < tomorrow);

                // Count this week's appointments
                vm.ThisWeekCount = _context.Appointments
                    .Count(a => a.DoctorId == doctor.DoctorId &&
                                a.AppointmentDate >= startOfWeek &&
                                a.AppointmentDate < endOfWeek);

                // Upcoming 5 appointments
                vm.UpcomingAppointments = _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.DoctorId == doctor.DoctorId && a.AppointmentDate >= DateTime.Now)
                    .OrderBy(a => a.AppointmentDate)
                    .Take(5)
                    .Select(a => new AppointmentInfo
                    {
                        AppointmentDate = a.AppointmentDate,
                        PatientName = a.Patient.Name,
                        Status = a.Status
                    })
                    .ToList();

                // ✅ Previous 5 appointments
                vm.PreviousAppointments = _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.DoctorId == doctor.DoctorId && a.AppointmentDate < DateTime.Now)
                    .OrderByDescending(a => a.AppointmentDate)
                    .Take(5)
                    .Select(a => new AppointmentInfo
                    {
                        AppointmentDate = a.AppointmentDate,
                        PatientName = a.Patient.Name,
                        Status = a.Status
                    })
                    .ToList();
            }

            return View(vm);
        }
    }
}
