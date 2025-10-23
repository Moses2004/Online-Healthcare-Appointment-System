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
    public class DoctorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DoctorsController(ApplicationDbContext context,
                                 UserManager<ApplicationUser> userManager,
                                 RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        // GET: Doctors
        public async Task<IActionResult> Index(string searchString)
        {
            var doctors = _context.Doctors
                .Include(d => d.Specialization)
                .Include(d => d.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                doctors = doctors.Where(d => d.Name.Contains(searchString));
            }

            return View(await doctors.ToListAsync());
        }


        // GET: Doctors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors
                .Include(d => d.Specialization)
                .Include(d => d.User)
                .FirstOrDefaultAsync(m => m.DoctorId == id);

            if (doctor == null)
            {
                return NotFound();
            }

            return View(doctor);
        }

        // GET: Doctors/Create
        public IActionResult Create()
        {
            ViewData["SpecializationId"] = new SelectList(_context.Specializations, "SpecializationId", "SpecializationName");
            return View();
        }

        // POST: Doctors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DoctorRegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewData["SpecializationId"] = new SelectList(_context.Specializations, "SpecializationId", "SpecializationName", model.SpecializationId);
                return View(model);
            }

            // Check if the email is already used
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                ViewData["SpecializationId"] = new SelectList(_context.Specializations, "SpecializationId", "SpecializationName", model.SpecializationId);
                return View(model);
            }

            //  Create new user for the doctor
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                FullName = model.FullName,
                RoleType = "Doctor",
                EmailConfirmed = true //  confirm automatically since admin creates
            };

            var userResult = await _userManager.CreateAsync(user, model.Password);

            if (!userResult.Succeeded)
            {
                foreach (var error in userResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                ViewData["SpecializationId"] = new SelectList(_context.Specializations, "SpecializationId", "SpecializationName", model.SpecializationId);
                return View(model);
            }

            // 3 Assign Doctor role (create if not exist)
            if (!await _roleManager.RoleExistsAsync("Doctor"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Doctor"));
            }

            await _userManager.AddToRoleAsync(user, "Doctor");

            // Create Doctor record linked to the new user
            var doctor = new Doctor
            {
                Name = model.FullName,
                UserId = user.Id,
                SpecializationId = model.SpecializationId,
                Availability = model.Availability,
                ConsultationFee = model.ConsultationFee,
                IsApproved = true
            };

            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Doctor '{doctor.Name}' created successfully with login: {user.Email}";

            return RedirectToAction(nameof(Index));
        }
    

        // GET: Doctors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
            {
                return NotFound();
            }

            ViewData["SpecializationId"] = new SelectList(_context.Specializations, "SpecializationId", "SpecializationName", doctor.SpecializationId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", doctor.UserId);
            return View(doctor);
        }

        // POST: Doctors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DoctorId,Name,SpecializationId,Availability,ConsultationFee,UserId,IsApproved")] Doctor doctor)
        {
            if (id != doctor.DoctorId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(doctor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DoctorExists(doctor.DoctorId))
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

            ViewData["SpecializationId"] = new SelectList(_context.Specializations, "SpecializationId", "SpecializationName", doctor.SpecializationId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Email", doctor.UserId);
            return View(doctor);
        }

        // GET: Doctors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors
                .Include(d => d.Specialization)
                .Include(d => d.User)
                .FirstOrDefaultAsync(m => m.DoctorId == id);

            if (doctor == null)
            {
                return NotFound();
            }

            return View(doctor);
        }

        // POST: Doctors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor != null)
            {
                _context.Doctors.Remove(doctor);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Approve Doctor
        public async Task<IActionResult> Approve(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null) return NotFound();

            doctor.IsApproved = true;
            _context.Update(doctor);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Remove Doctor
        public async Task<IActionResult> Remove(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null) return NotFound();

            _context.Doctors.Remove(doctor);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool DoctorExists(int id)
        {
            return _context.Doctors.Any(e => e.DoctorId == id);
        }
    }
}
