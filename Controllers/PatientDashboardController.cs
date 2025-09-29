using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Online_Healthcare_Appointment_System.Data;
using Online_Healthcare_Appointment_System.Models;
using Online_Healthcare_Appointment_System.Models.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace Online_Healthcare_Appointment_System.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PatientDashboardController(ApplicationDbContext context,
                                          UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var patient = _context.Patients.FirstOrDefault(p => p.UserId == user.Id);

            var vm = new PatientDashboardVM
            {
                PatientName = patient?.Name ?? user.FullName
            };

            if (patient != null)
            {
                // Date ranges
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                var endOfWeek = startOfWeek.AddDays(7);

                // Counts
                vm.TodayCount = _context.Appointments
                    .Count(a => a.PatientId == patient.PatientId &&
                                a.AppointmentDate >= today &&
                                a.AppointmentDate < tomorrow);

                vm.ThisWeekCount = _context.Appointments
                    .Count(a => a.PatientId == patient.PatientId &&
                                a.AppointmentDate >= startOfWeek &&
                                a.AppointmentDate < endOfWeek);

                // Upcoming (next 5)
                vm.UpcomingAppointments = _context.Appointments
                    .Where(a => a.PatientId == patient.PatientId && a.AppointmentDate >= today)
                    .OrderBy(a => a.AppointmentDate)
                    .Take(5)
                    .Select(a => new PatientAppointmentInfo
                    {
                         AppointmentDate = a.AppointmentDate,
                        DoctorName = _context.Doctors
                                      .Where(d => d.DoctorId == a.DoctorId)
                                      .Select(d => d.Name)
                                      .FirstOrDefault(),
                        Status = a.Status
                    })
                    .ToList();
            }

            return View(vm);
        }

    }
}
