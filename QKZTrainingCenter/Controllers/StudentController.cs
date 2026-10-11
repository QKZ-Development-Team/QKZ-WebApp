using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QKZTrainingCenter.Data;
using QKZTrainingCenter.Models;

namespace QKZTrainingCenter.Controllers
{
    public class StudentController : Controller
    {
        private readonly QKZDbContext _context;

        public StudentController(QKZDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.FullName = HttpContext.Session.GetString("FullName");

            return View();
        }

       
[HttpGet]
public async Task<IActionResult> Registration()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var roleId = HttpContext.Session.GetInt32("RoleID");

            // Redirect users who are not logged in.
            if (userId == null || roleId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Administrators must use the Admin Dashboard.
            if (roleId == 1)
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            // Only students can access their registration details.
            if (roleId != 2)
            {
                return Forbid();
            }

            var registration = await (
                from user in _context.Users
                join student in _context.Students
                    on user.UserID equals student.UserID
                join reg in _context.Registrations
                    on student.StudentID equals reg.StudentID
                join course in _context.Courses
                    on reg.CourseID equals course.CourseID
                where user.UserID == userId.Value
                orderby reg.RegistrationDate descending
                select new StudentRegistrationDetailsViewModel
                {
                    FullName = user.FullName,
                    EmailAddress = user.EmailAddress,
                    IDNumber = student.IDNumber,
                    Address = student.Address ?? string.Empty,
                    CourseName = course.CourseName ?? string.Empty,
                    CourseTitle = course.Title ?? string.Empty,
                    RegistrationDate = reg.RegistrationDate,
                    RegistrationStatus = reg.RegistrationStatus
                }
            ).FirstOrDefaultAsync();

            if (registration == null)
            {
                TempData["RegistrationMessage"] =
                    "No registration was found for your account. Please register for a training course or contact QKZ Training Center for assistance.";

                return RedirectToAction("Dashboard", "Student");
            }

            return View(registration);
        }


     
[HttpGet]
public async Task<IActionResult> Materials()
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserID == userId.Value);

            if (student == null)
            {
                return View(new List<StudyMaterial>());
            }

            var courseIds = await _context.Registrations
                .Where(r => r.StudentID == student.StudentID
                            && r.RegistrationStatus != "Cancelled")
                .Where(r => r.CourseID != null)
                .Select(r => r.CourseID!.Value)
                .Distinct()
                .ToListAsync();

            var materials = await _context.StudyMaterials
                .Where(m => courseIds.Contains(m.CourseID) && m.IsActive)
                .OrderBy(m => m.Title)
                .ToListAsync();

            return View(materials);
        }
    

    [HttpGet]
        public async Task<IActionResult> Schedule()
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var schedule = await (
                from student in _context.Students
                join registration in _context.Registrations
                    on student.StudentID equals registration.StudentID
                join scheduleItem in _context.Schedules
                    on registration.CourseID equals scheduleItem.CourseID
                join course in _context.Courses
                    on scheduleItem.CourseID equals course.CourseID
                where student.UserID == userId.Value
                      && scheduleItem.IsActive
                orderby scheduleItem.StartDateTime
                select new
                {
                    CourseName = course.CourseName,
                    Title = scheduleItem.Title,
                    StartDateTime = scheduleItem.StartDateTime,
                    EndDateTime = scheduleItem.EndDateTime
                }
            ).ToListAsync();

            return View(schedule);
        }
    }

}
