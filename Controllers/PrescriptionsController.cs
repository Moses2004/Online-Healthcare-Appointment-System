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
    [Authorize]
    public class PrescriptionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PrescriptionsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 🩺 Helper: Get current DoctorId
        private async Task<int?> GetCurrentDoctorIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            var doctor = await _context.Doctors
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == user.Id);
            return doctor?.DoctorId;
        }

        // 👩‍🦰 Helper: Get current PatientId
        private async Task<int?> GetCurrentPatientIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            var patient = await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id);
            return patient?.PatientId;
        }

        // ============================================================
        // ====================== ROLE-AWARE INDEX ====================
        // ============================================================

        [Authorize]
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Doctor"))
            {
                var doctorId = await GetCurrentDoctorIdAsync();
                if (doctorId == null) return Forbid();

                var list = await _context.Prescriptions
                    .Include(p => p.Appointment)
                        .ThenInclude(a => a.Patient)
                    .Where(p => p.Appointment.DoctorId == doctorId)
                    .OrderByDescending(p => p.DateIssued)
                    .ToListAsync();

                return View(list);
            }
            else if (User.IsInRole("Patient"))
            {
                var patientId = await GetCurrentPatientIdAsync();
                if (patientId == null) return Forbid();

                var list = await _context.Prescriptions
                    .Include(p => p.Appointment)
                        .ThenInclude(a => a.Doctor)
                    .Where(p => p.Appointment.PatientId == patientId)
                    .OrderByDescending(p => p.DateIssued)
                    .ToListAsync();

                return View(list);
            }

            return Forbid();
        }

        // ============================================================
        // ====================== DETAILS =============================
        // ============================================================

        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Doctor)
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Patient)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id);

            if (prescription == null)
                return NotFound();

            if (User.IsInRole("Doctor"))
            {
                var doctorId = await GetCurrentDoctorIdAsync();
                if (doctorId == null || prescription.Appointment.DoctorId != doctorId)
                    return Forbid();
            }
            else if (User.IsInRole("Patient"))
            {
                var patientId = await GetCurrentPatientIdAsync();
                if (patientId == null || prescription.Appointment.PatientId != patientId)
                    return Forbid();
            }
            else
            {
                return Forbid();
            }

            return View(prescription);
        }

        // ============================================================
        // ====================== CREATE ==============================
        // ============================================================

        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> Create(int? appointmentId)
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (doctorId == null) return Forbid();

            // ✅ Only show appointments that are APPROVED and have no prescription yet
            var eligible = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Include(a => a.Prescription)
                .Where(a => a.DoctorId == doctorId &&
                            a.Status == "Approved" &&
                            a.Prescription == null)
                .OrderByDescending(a => a.AppointmentDate)
                .Select(a => new
                {
                    a.AppointmentId,
                    Label = $"Appt #{a.AppointmentId} — {a.AppointmentDate:g} — {a.Patient.Email}"
                })
                .ToListAsync();

            if (!eligible.Any())
                return BadRequest("No eligible appointments to prescribe for.");

            ViewBag.AppointmentId = new SelectList(eligible, "AppointmentId", "Label", appointmentId);
            return View(new Prescription { DateIssued = DateTime.UtcNow });
        }

        [Authorize(Roles = "Doctor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AppointmentId,DoctorNotes,PrescriptionDetails")] Prescription model)
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (doctorId == null) return Forbid();

            var appt = await _context.Appointments
                .Include(a => a.Prescription)
                .FirstOrDefaultAsync(a => a.AppointmentId == model.AppointmentId);

            if (appt == null)
                ModelState.AddModelError("", "Appointment not found.");
            else
            {
                if (appt.DoctorId != doctorId)
                    ModelState.AddModelError("", "You can only prescribe for your own appointments.");
                if (appt.Status != "Approved")
                    ModelState.AddModelError("", "Prescription allowed only for approved appointments.");
                if (appt.Prescription != null)
                    ModelState.AddModelError("", "This appointment already has a prescription.");
            }

            ModelState.Remove(nameof(Prescription.Appointment));

            if (!ModelState.IsValid)
            {
                var eligible = await _context.Appointments
                    .AsNoTracking()
                    .Include(a => a.Patient)
                    .Include(a => a.Prescription)
                    .Where(a => a.DoctorId == doctorId &&
                                a.Status == "Approved" &&
                                a.Prescription == null)
                    .OrderByDescending(a => a.AppointmentDate)
                    .Select(a => new
                    {
                        a.AppointmentId,
                        Label = $"Appt #{a.AppointmentId} — {a.AppointmentDate:g} — {a.Patient.Email}"
                    })
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

        // ============================================================
        // ====================== EDIT & DELETE =======================
        // ============================================================

        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> Edit(int id)
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (doctorId == null) return Forbid();

            var item = await _context.Prescriptions
                .Include(p => p.Appointment)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id && p.Appointment.DoctorId == doctorId);

            if (item == null) return NotFound();
            return View(item);
        }

        [Authorize(Roles = "Doctor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PrescriptionId,AppointmentId,DoctorNotes,PrescriptionDetails")] Prescription model)
        {
            if (id != model.PrescriptionId) return NotFound();

            var doctorId = await GetCurrentDoctorIdAsync();
            if (doctorId == null) return Forbid();

            var entity = await _context.Prescriptions
                .Include(p => p.Appointment)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id && p.Appointment.DoctorId == doctorId);

            if (entity == null) return NotFound();

            entity.DoctorNotes = model.DoctorNotes;
            entity.PrescriptionDetails = model.PrescriptionDetails;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = entity.PrescriptionId });
        }

        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> Delete(int id)
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (doctorId == null) return Forbid();

            var item = await _context.Prescriptions
                .Include(p => p.Appointment)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id && p.Appointment.DoctorId == doctorId);

            if (item == null) return NotFound();
            return View(item);
        }

        [Authorize(Roles = "Doctor")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (doctorId == null) return Forbid();

            var entity = await _context.Prescriptions
                .Include(p => p.Appointment)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id && p.Appointment.DoctorId == doctorId);

            if (entity == null) return NotFound();

            _context.Prescriptions.Remove(entity);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
