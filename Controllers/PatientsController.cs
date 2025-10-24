using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    [Authorize]
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public PatientsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }


        // GET: Patients
        [Authorize(Roles = "Admin,Doctor")]
     
        public async Task<IActionResult> Index(string searchEmail)
        {
            IQueryable<Patient> patients = _context.Patients
                .Include(p => p.User)
                .AsQueryable();

            var userEmail = User.Identity.Name;

            //  Admin can see all patients
            if (User.IsInRole("Admin"))
            {
                if (!string.IsNullOrEmpty(searchEmail))
                    patients = patients.Where(p => p.User.Email.Contains(searchEmail));

                ViewData["SearchEmail"] = searchEmail;
                return View(await patients.ToListAsync());
            }

            //  Doctor can see only patients who made appointments with him
            else if (User.IsInRole("Doctor"))
            {
                var doctor = await _context.Doctors
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.User.Email == userEmail);

                if (doctor == null) return Forbid();

                patients = _context.Appointments
                    .Where(a => a.DoctorId == doctor.DoctorId)
                    .Include(a => a.Patient)
                        .ThenInclude(p => p.User)
                    .Select(a => a.Patient)
                    .Distinct();

                return View(await patients.ToListAsync());
            }

            // Patient can only view their own profile
            else if (User.IsInRole("Patient"))
            {
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.User.Email == userEmail);

                if (patient == null) return Forbid();

                return View(new List<Patient> { patient });
            }

            // If no valid role
            return Forbid();
        }


        // Redirect to Identity Manage Page
        public IActionResult ManageProfile()
        {
            return Redirect("/Identity/Account/Manage");
        }

        // GET: Patients/Details/5
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.PatientId == id);
            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }

        // GET: Patients/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email");
            return View();
        }

        // POST: Patients/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Patient patient, string Password)
        {
            // 1️⃣ Remove these from validation (because we’ll fill them manually)
            ModelState.Remove("User");
            ModelState.Remove("UserId");

            if (ModelState.IsValid)
            {
                // 2️⃣ Step 1: Create the Identity user
                var user = new ApplicationUser
                {
                    UserName = patient.Email,
                    Email = patient.Email,
                    FullName = patient.Name,
                    PhoneNumber = patient.Phone,
                    RoleType = "Patient"
                };

                var result = await _userManager.CreateAsync(user, Password);

                if (result.Succeeded)
                {
                    // 3️⃣ Step 2: Assign "Patient" role
                    if (!await _roleManager.RoleExistsAsync("Patient"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Patient"));
                    }
                    await _userManager.AddToRoleAsync(user, "Patient");

                    // 4️⃣ Step 3: Link user to patient record
                    patient.UserId = user.Id;
                    patient.User = user;

                    _context.Patients.Add(patient);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }

            return View(patient);
        }



        // GET: Patients/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", patient.UserId);
            return View(patient);
        }

        // POST: Patients/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PatientId,Name,DOB,Gender,Address,UserId")] Patient patient)
        {
            if (id != patient.PatientId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(patient);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientExists(patient.PatientId))
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
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", patient.UserId);
            return View(patient);
        }

        // GET: Patients/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.PatientId == id);
            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }

        // POST: Patients/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient != null)
            {
                _context.Patients.Remove(patient);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PatientExists(int id)
        {
            return _context.Patients.Any(e => e.PatientId == id);
        }
    }
}
