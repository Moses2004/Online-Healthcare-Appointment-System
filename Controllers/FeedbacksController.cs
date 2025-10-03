using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Online_Healthcare_Appointment_System.Data;
using Online_Healthcare_Appointment_System.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Online_Healthcare_Appointment_System.Controllers
{
    public class FeedbacksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FeedbacksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Feedbacks
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Feedbacks.Include(f => f.Doctor).Include(f => f.Patient);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Feedbacks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var feedback = await _context.Feedbacks
                .Include(f => f.Doctor)
                .Include(f => f.Patient)
                .FirstOrDefaultAsync(m => m.FeedbackId == id);
            if (feedback == null)
            {
                return NotFound();
            }

            return View(feedback);
        }

        // GET: Feedbacks/Create
        [Authorize(Roles = "Patient")]
        public IActionResult Create()
        {
            // Only show doctors to choose from (names, not IDs)
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

            // Do NOT expose Patient dropdown anymore
            return View();
        }

        // POST: Feedbacks/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Patient")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DoctorId,Comments,Rating")] Feedback feedback)
        {
            // Find logged-in patient
            var userEmail = User.Identity?.Name;
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.User.Email == userEmail);

            if (patient == null)
            {
                ModelState.AddModelError("", "No patient record linked to this account. Please contact admin.");
            }
            else
            {
                // Auto-assign secure fields
                feedback.PatientId = patient.PatientId;
                feedback.DateSubmitted = DateTime.Now;

                // Clear model-state keys that aren’t bound from the form
                ModelState.Remove("PatientId");
                ModelState.Remove("Patient");
                ModelState.Remove("Doctor"); // navigation prop safeguard
                ModelState.Remove("DateSubmitted");
            }

            if (ModelState.IsValid)
            {
                _context.Add(feedback);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Rebuild doctor dropdown on validation failure
            ViewData["DoctorId"] = new SelectList(
                _context.Doctors
                    .Where(d => d.IsApproved)
                    .Include(d => d.Specialization)
                    .Select(d => new {
                        d.DoctorId,
                        DisplayName = d.Name + " (" + d.Specialization.SpecializationName + ")"
                    }),
                "DoctorId",
                "DisplayName",
                feedback.DoctorId
            );

            return View(feedback);
        }



        // GET: Feedbacks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback == null) return NotFound();

            ViewData["DoctorId"] = new SelectList(_context.Doctors, "DoctorId", "Name", feedback.DoctorId);
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "Name", feedback.PatientId);
            return View(feedback);
        }

        // POST: Feedbacks/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("FeedbackId,PatientId,DoctorId,Comments,Rating,DateSubmitted")] Feedback feedback)
        {
            if (id != feedback.FeedbackId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(feedback);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FeedbackExists(feedback.FeedbackId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["DoctorId"] = new SelectList(_context.Doctors, "DoctorId", "DoctorId", feedback.DoctorId);
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "PatientId", feedback.PatientId);
            return View(feedback);
        }

        // GET: Feedbacks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var feedback = await _context.Feedbacks
                .Include(f => f.Doctor)
                .Include(f => f.Patient)
                .FirstOrDefaultAsync(m => m.FeedbackId == id);
            if (feedback == null)
            {
                return NotFound();
            }

            return View(feedback);
        }

        // POST: Feedbacks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback != null)
            {
                _context.Feedbacks.Remove(feedback);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FeedbackExists(int id)
        {
            return _context.Feedbacks.Any(e => e.FeedbackId == id);
        }
    }
}
