using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Online_Healthcare_Appointment_System.Data;
using Online_Healthcare_Appointment_System.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Online_Healthcare_Appointment_System.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class PrescriptionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PrescriptionsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Helper: get current DoctorId from logged-in user.
        // Assumes there's a Doctor table linking AspNetUsers -> DoctorId.
        private async Task<int?> GetCurrentDoctorIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            // Adjust if  Doctor entity uses a different FK field name
            var doctor = await _context.Doctors
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == user.Id);
            return doctor?.DoctorId;
        }

        // Only show prescriptions of this doctor
        public async Task<IActionResult> Index()
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (doctorId == null) return Forbid();

            var list = await _context.Prescriptions
                .Include(p => p.Appointment)
                .Where(p => p.Appointment.DoctorId == doctorId)
                .OrderByDescending(p => p.DateIssued)
                .ToListAsync();

            return View(list);
        }

        public async Task<IActionResult> Details(int id)
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (doctorId == null) return Forbid();

            var item = await _context.Prescriptions
                .Include(p => p.Appointment)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id && p.Appointment.DoctorId == doctorId);

            if (item == null) return NotFound();
            return View(item);
        }

        // Create by selecting ONE eligible appointment or passing ?appointmentId=#
        public async Task<IActionResult> Create(int? appointmentId)
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (doctorId == null) return Forbid();

            // eligible = only this doctor's completed/done appts with no prescription yet
            var eligible = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)           // email in label
                .Include(a => a.Prescription)
                .Where(a => a.DoctorId == doctorId &&
                            (a.Status == "Completed" || a.Status == "Done") &&
                            a.Prescription == null)
                .OrderByDescending(a => a.AppointmentDate)
                .Select(a => new
                {
                    a.AppointmentId,
                    Label = $"Appt #{a.AppointmentId} — {a.AppointmentDate:g}" +
                            (a.Patient != null ? $" — {a.Patient.Email}" : "")
                })
                .ToListAsync();

            if (appointmentId.HasValue)
            {
                // validate the provided appointment
                var exists = eligible.Any(e => e.AppointmentId == appointmentId.Value);
                if (!exists)
                    return BadRequest("This appointment is not eligible for a prescription.");

                ViewBag.AppointmentId = new SelectList(eligible, "AppointmentId", "Label", appointmentId.Value);
                return View(new Prescription { AppointmentId = appointmentId.Value, DateIssued = DateTime.UtcNow });
            }

            if (!eligible.Any())
                return BadRequest("No eligible appointments to prescribe for.");

            // ✅ Preselect the first eligible appt so the select has a value
            var firstId = eligible.First().AppointmentId;
            ViewBag.AppointmentId = new SelectList(eligible, "AppointmentId", "Label", firstId);

            return View(new Prescription { AppointmentId = firstId, DateIssued = DateTime.UtcNow });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AppointmentId,DoctorNotes,PrescriptionDetails")] Prescription model)
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (doctorId == null) return Forbid();

            // Load and validate appointment ownership/status/no-duplicate
            var appt = await _context.Appointments
                .Include(a => a.Prescription)
                .FirstOrDefaultAsync(a => a.AppointmentId == model.AppointmentId);

            if (appt == null)
                ModelState.AddModelError(nameof(model.AppointmentId), "Appointment not found.");
            else
            {
                if (appt.DoctorId != doctorId)
                    ModelState.AddModelError("", "You can only prescribe for your own appointments.");
                if (!(appt.Status == "Completed" || appt.Status == "Done"))
                    ModelState.AddModelError("", "Prescription allowed only for completed appointments.");
                if (appt.Prescription != null)
                    ModelState.AddModelError("", "This appointment already has a prescription.");
            }
            ModelState.Remove(nameof(Prescription.Appointment));  // ignore nav prop

            if (!ModelState.IsValid)
            {
                // rebuild select list (with the chosen id selected)
                var eligible = await _context.Appointments
                    .AsNoTracking()
                    .Include(a => a.Patient)
                    .Include(a => a.Prescription)
                    .Where(a => a.DoctorId == doctorId &&
                                (a.Status == "Completed" || a.Status == "Done") &&
                                a.Prescription == null)
                    .OrderByDescending(a => a.AppointmentDate)
                    .Select(a => new { a.AppointmentId, Label = $"Appt #{a.AppointmentId} — {a.AppointmentDate:g}" + (a.Patient != null ? $" — {a.Patient.Email}" : "") })
                    .ToListAsync();

                ViewBag.AppointmentId = new SelectList(eligible, "AppointmentId", "Label", model.AppointmentId);
                return View(model);
            }

            var entity = new Prescription
            {
                AppointmentId = model.AppointmentId,
                DoctorNotes = model.DoctorNotes,
                PrescriptionDetails = model.PrescriptionDetails,
                DateIssued = DateTime.UtcNow
            };
            _context.Prescriptions.Add(entity);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = entity.PrescriptionId });
        }


    }
}
