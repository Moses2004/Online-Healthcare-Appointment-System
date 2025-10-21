using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Healthcare_Appointment_System.Data;
using Online_Healthcare_Appointment_System.Models;
using System.Linq;
using System;

namespace Online_Healthcare_Appointment_System.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            //  Chart 1: Appointments per Doctor
            var doctorReport = _context.Appointments
                .Include(a => a.Doctor)
                .Where(a => a.Status == "Completed")
                .GroupBy(a => a.Doctor.Name)
                .Select(g => new ReportViewModel
                {
                    DoctorName = g.Key,
                    AppointmentCount = g.Count()
                })
                .ToList();

            //  Chart 2: Total Payments per Month
            var paymentReport = _context.Payments
                .AsEnumerable() //  Force evaluation in memory
                .GroupBy(p => new { Year = p.PaymentDate.Year, Month = p.PaymentDate.Month })
                .Select(g => new
                {
                    Month = $"{g.Key.Month:D2}/{g.Key.Year}",
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .OrderBy(g => g.Month)
                .ToList();

            // PATIENT STATISTICS 
            var patients = _context.Patients.AsEnumerable();

            // Gender distribution
            var genderStats = patients
                .GroupBy(p => p.Gender ?? "Unknown")
                .Select(g => new { Gender = g.Key, Count = g.Count() })
                .ToList();

            // Age group distribution
            DateTime today = DateTime.Today;
            var ageStats = patients
                .Select(p => new
                {
                    Age = (int)((today - p.DOB).TotalDays / 365.25)
                })
                .GroupBy(a =>
                    a.Age < 18 ? "0-17" :
                    a.Age < 31 ? "18-30" :
                    a.Age < 51 ? "31-50" : "51+")
                .Select(g => new { Group = g.Key, Count = g.Count() })
                .OrderBy(g => g.Group)
                .ToList();

            // Send data to ViewBag for charts
            ViewBag.GenderLabels = genderStats.Select(g => g.Gender).ToList();
            ViewBag.GenderCounts = genderStats.Select(g => g.Count).ToList();

            ViewBag.AgeLabels = ageStats.Select(a => a.Group).ToList();
            ViewBag.AgeCounts = ageStats.Select(a => a.Count).ToList();


            ViewBag.PaymentMonths = paymentReport.Select(p => p.Month).ToList();
            ViewBag.PaymentTotals = paymentReport.Select(p => p.TotalAmount).ToList();

            return View(doctorReport);
        }
    }
}


