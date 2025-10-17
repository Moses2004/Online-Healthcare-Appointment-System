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
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Online_Healthcare_Appointment_System.Controllers
{
    [Authorize]
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        public PaymentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Payments
        // GET: Payments
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            IQueryable<Payment> query = _context.Payments
                .Include(p => p.Appointment)
                .ThenInclude(a => a.Patient)
                .Include(p => p.Appointment.Doctor);

            if (User.IsInRole("Admin"))
            {
                return View(await query.ToListAsync());
            }
            else if (User.IsInRole("Patient"))
            {
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (patient == null) return Forbid();

                query = query.Where(p => p.Appointment.PatientId == patient.PatientId);
                return View(await query.ToListAsync());
            }

            //  Block doctors from payments
            return Forbid();
        }



        // GET: Payments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Appointment)
                .FirstOrDefaultAsync(m => m.PaymentId == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // GET: Payments/Create
        public IActionResult Create(int appointmentId)
        {
            var appointment = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefault(a => a.AppointmentId == appointmentId);

            if (appointment == null) return NotFound();

            var payment = new Payment
            {
                AppointmentId = appointment.AppointmentId,
                PaymentDate = DateTime.Now
            };

            ViewBag.PatientName = appointment.Patient?.Name;
            ViewBag.DoctorName = appointment.Doctor?.Name;
            return View(payment);
        }


        // POST: Payments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AppointmentId,Amount,PaymentMethod")] Payment payment)
        {
            //  Ignore navigation props
            ModelState.Remove("Appointment");
            ModelState.Remove("Status");

            if (ModelState.IsValid)
            {
                payment.PaymentDate = DateTime.Now;
                payment.Status = "Paid";

                _context.Add(payment);

                // After successful payment — update appointment status
                var appointment = await _context.Appointments.FindAsync(payment.AppointmentId);
                if (appointment != null)
                {
                    if (appointment.Status == "Approved")
                    {
                        appointment.Status = "Completed";
                        _context.Update(appointment);
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Payment successful! Appointment marked as Completed.";
                return RedirectToAction("Details", "Appointments", new { id = payment.AppointmentId });
            }

            //  Debug invalid fields
            foreach (var item in ModelState)
            {
                if (item.Value.ValidationState == Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid)
                {
                    Console.WriteLine($"❌ Invalid field: {item.Key}");
                }
            }

            return View(payment);
        }



        // GET: Payments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
                return NotFound();
            }
            ViewData["AppointmentId"] = new SelectList(_context.Appointments, "AppointmentId", "AppointmentId", payment.AppointmentId);
            return View(payment);
        }

        // POST: Payments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PaymentId,AppointmentId,Amount,PaymentDate,PaymentMethod,Status")] Payment payment)
        {
            if (id != payment.PaymentId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(payment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PaymentExists(payment.PaymentId))
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
            ViewData["AppointmentId"] = new SelectList(_context.Appointments, "AppointmentId", "AppointmentId", payment.AppointmentId);
            return View(payment);
        }

        // GET: Payments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Appointment)
                .FirstOrDefaultAsync(m => m.PaymentId == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // POST: Payments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PaymentExists(int id)
        {
            return _context.Payments.Any(e => e.PaymentId == id);
        }
    }
}
