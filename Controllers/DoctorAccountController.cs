using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Online_Healthcare_Appointment_System.Data;
using Online_Healthcare_Appointment_System.Models;
using System.Threading.Tasks;

namespace Online_Healthcare_Appointment_System.Controllers
{
    public class DoctorAccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public DoctorAccountController(ApplicationDbContext context,
                                       UserManager<ApplicationUser> userManager,
                                       SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: DoctorAccount/Register
        public IActionResult Register()
        {
            ViewData["SpecializationId"] = new SelectList(_context.Specializations, "SpecializationId", "SpecializationName");
            return View();
        }

        // POST: DoctorAccount/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(DoctorRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Step 1: Create AspNetUser
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    RoleType = "Doctor"
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Step 2: Create Doctor profile linked to AspNetUsers
                    var doctor = new Doctor
                    {
                        Name = model.FullName,
                        UserId = user.Id,                // link to AspNetUsers
                        SpecializationId = model.SpecializationId,
                        ConsultationFee = model.ConsultationFee,
                        Availability = true
                    };

                    _context.Doctors.Add(doctor);
                    await _context.SaveChangesAsync();  // critical

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewData["SpecializationId"] = new SelectList(_context.Specializations, "SpecializationId", "SpecializationName", model.SpecializationId);
            return View(model);
        }

    }
}
