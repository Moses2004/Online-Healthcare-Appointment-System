using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Online_Healthcare_Appointment_System.Data;
using Online_Healthcare_Appointment_System.Models;
using Microsoft.AspNetCore.Authorization;

namespace Online_Healthcare_Appointment_System.Controllers
{
    [Authorize]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppointmentsController(ApplicationDbContext context)
        {
            _context = context;
        }


        //  INDEX

        public async Task<IActionResult> Index(string searchString)
        {
            IQueryable<Appointment> query = _context.Appointments
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.Specialization)
                .Include(a => a.Patient);

            //  Admin can see all appointments
            if (User.IsInRole("Admin"))
            {
                if (!string.IsNullOrEmpty(searchString))
                {
                    string lowerSearch = searchString.ToLower();
                    query = query.Where(a =>
                        a.Doctor.Name.ToLower().Contains(lowerSearch) ||
                        a.Patient.Name.ToLower().Contains(lowerSearch));
                }

                return View(await query.OrderByDescending(a => a.AppointmentDate).ToListAsync());
            }

            //  Doctor view
            else if (User.IsInRole("Doctor"))
            {
                var userEmail = User.Identity.Name;
                var doctor = await _context.Doctors
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.User.Email == userEmail);

                if (doctor == null) return Forbid();

                query = query.Where(a => a.DoctorId == doctor.DoctorId);
            }

            //  Patient view
            else if (User.IsInRole("Patient"))
            {
                var userEmail = User.Identity.Name;
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.User.Email == userEmail);

                if (patient == null) return Forbid();

                query = query.Where(a => a.PatientId == patient.PatientId);
            }
            else
            {
                return Forbid();
            }

            var list = await query.OrderByDescending(a => a.AppointmentDate).ToListAsync();
            return View(list);
        }


        // DETAILS 


        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Include(a => a.Payments)
                .FirstOrDefaultAsync(m => m.AppointmentId == id);

            if (appointment == null) return NotFound();

            //  Restrict view access
            if (User.IsInRole("Doctor"))
            {
                var userEmail = User.Identity.Name;
                var doctor = await _context.Doctors
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.User.Email == userEmail);

                if (doctor == null || doctor.DoctorId != appointment.DoctorId)
                    return Forbid();
            }
            else if (User.IsInRole("Patient"))
            {
                var userEmail = User.Identity.Name;
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.User.Email == userEmail);

                if (patient == null || patient.PatientId != appointment.PatientId)
                    return Forbid();
            }

            return View(appointment);
        }

        
        // ==================== CREATE =================================
      

        public IActionResult Create()
        {
            ViewData["DoctorId"] = new SelectList(
                _context.Doctors
                    .Where(d => d.IsApproved)
                    .Include(d => d.Specialization)
                    .Select(d => new {
                        d.DoctorId,
                        DisplayName = d.Name + " (" + d.Specialization.SpecializationName + ")"
                    }),
                "DoctorId",
                "DisplayName"
            );

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AppointmentDate,Notes,DoctorId")] Appointment appointment)
        {
            var userEmail = User.Identity.Name;
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.User.Email == userEmail);

            if (patient == null)
            {
                ModelState.AddModelError("", $"⚠ Could not find patient for email: {userEmail}");
            }
            else
            {
                appointment.PatientId = patient.PatientId;
                appointment.Status = "Pending";

                ModelState.Remove("PatientId");
                ModelState.Remove("Status");
                ModelState.Remove("Patient");
                ModelState.Remove("Doctor");
                ModelState.Remove("Prescription");
            }

            if (ModelState.IsValid)
            {
                _context.Add(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "PatientDashboard");
            }

            ViewData["DoctorId"] = new SelectList(
                _context.Doctors.Where(d => d.IsApproved)
                .Include(d => d.Specialization)
                .Select(d => new {
                    d.DoctorId,
                    DisplayName = d.Name + " (" + d.Specialization.SpecializationName + ")"
                }),
                "DoctorId",
                "DisplayName",
                appointment.DoctorId
            );

            return View(appointment);
        }

     
        //  EDIT 
       

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            //  Restrict edit access
            if (User.IsInRole("Doctor"))
            {
                var userEmail = User.Identity.Name;
                var doctor = await _context.Doctors
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.User.Email == userEmail);

                if (doctor == null || doctor.DoctorId != appointment.DoctorId)
                    return Forbid();
            }
            else if (User.IsInRole("Patient"))
            {
                // Patients cannot edit appointments
                return Forbid();
            }

            ViewData["DoctorId"] = new SelectList(
                _context.Doctors.Where(d => d.IsApproved),
                "DoctorId",
                "Name",
                appointment.DoctorId
            );

            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "PatientId", appointment.PatientId);
            return View(appointment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AppointmentId,PatientId,DoctorId,AppointmentDate,Status,Notes")] Appointment appointment)
        {
            if (id != appointment.AppointmentId) return NotFound();

            // ✅ Remove invalid fields from validation
            ModelState.Remove("Doctor");
            ModelState.Remove("Patient");
            ModelState.Remove("Prescription");

            if (ModelState.IsValid)
            {
                var existing = await _context.Appointments.FindAsync(id);
                if (existing == null) return NotFound();

                // ✅ Restrict who can edit
                if (User.IsInRole("Doctor"))
                {
                    var userEmail = User.Identity.Name;
                    var doctor = await _context.Doctors
                        .Include(d => d.User)
                        .FirstOrDefaultAsync(d => d.User.Email == userEmail);

                    if (doctor == null || doctor.DoctorId != existing.DoctorId)
                        return Forbid();
                }
                else if (User.IsInRole("Patient"))
                {
                    return Forbid();
                }

                //  Apply updates
                existing.DoctorId = appointment.DoctorId;
                existing.PatientId = appointment.PatientId;
                existing.AppointmentDate = appointment.AppointmentDate;
                existing.Status = appointment.Status;
                existing.Notes = appointment.Notes;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Appointment updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewData["DoctorId"] = new SelectList(_context.Doctors, "DoctorId", "Name", appointment.DoctorId);
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "PatientId", appointment.PatientId);
            return View(appointment);
        }

    
        //  DELETE 
      

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(m => m.AppointmentId == id);

            if (appointment == null) return NotFound();

            //  Restrict delete access
            if (User.IsInRole("Doctor"))
            {
                var userEmail = User.Identity.Name;
                var doctor = await _context.Doctors
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.User.Email == userEmail);

                if (doctor == null || doctor.DoctorId != appointment.DoctorId)
                    return Forbid();
            }
            else if (User.IsInRole("Patient"))
            {
                return Forbid();
            }

            return View(appointment);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            // ✅ Restrict delete permission
            if (User.IsInRole("Doctor"))
            {
                var userEmail = User.Identity.Name;
                var doctor = await _context.Doctors
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.User.Email == userEmail);

                if (doctor == null || doctor.DoctorId != appointment.DoctorId)
                    return Forbid();
            }
            else if (User.IsInRole("Patient"))
            {
                return Forbid();
            }

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.AppointmentId == id);
        }
    }
}
