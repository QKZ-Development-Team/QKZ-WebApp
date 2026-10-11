using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QKZTrainingCenter.Data;
using QKZTrainingCenter.Models;

namespace QKZTrainingCenter.Controllers
{
    public class RegistrationController : Controller
    {
        private readonly QKZDbContext _context;
        private readonly IPasswordHasher<QKZUser> _passwordHasher;

        public RegistrationController(
            QKZDbContext context,
            IPasswordHasher<QKZUser> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new StudentRegistrationViewModel
            {
                ComboPackages = await _context.ComboPackages
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.PackageID)
                    .ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            StudentRegistrationViewModel model)
        {
            // Reload packages if the form needs to be displayed again.
            async Task ReloadPackages()
            {
                model.ComboPackages = await _context.ComboPackages
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.PackageID)
                    .ToListAsync();
            }

            if (!ModelState.IsValid)
            {
                await ReloadPackages();
                return View(model);
            }

            // Identify whether the selection is a grade or a package.
            Course? course = null;
            ComboPackage? package = null;

            if (model.TrainingSelection.StartsWith(
                    "Package-", StringComparison.OrdinalIgnoreCase))
            {
                var packageIdText = model.TrainingSelection["Package-".Length..];

                if (!int.TryParse(packageIdText, out var packageId))
                {
                    ModelState.AddModelError(
                        nameof(model.TrainingSelection),
                        "Please select a valid training option.");

                    await ReloadPackages();
                    return View(model);
                }

                package = await _context.ComboPackages
                    .FirstOrDefaultAsync(p =>
                        p.PackageID == packageId && p.IsActive);

                if (package == null)
                {
                    ModelState.AddModelError(
                        nameof(model.TrainingSelection),
                        "The selected combo package is unavailable.");

                    await ReloadPackages();
                    return View(model);
                }

                model.PackageID = package.PackageID;
                model.Grade = null;
            }
            else
            {
                var allowedGrades = new[]
                {
                    "Grade E", "Grade D", "Grade C", "Grade B", "Grade A"
                };

                if (!allowedGrades.Contains(
                        model.TrainingSelection,
                        StringComparer.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(
                        nameof(model.TrainingSelection),
                        "Please select a valid grade or combo package.");

                    await ReloadPackages();
                    return View(model);
                }

                course = await _context.Courses.FirstOrDefaultAsync(c =>
                    c.CourseName == model.TrainingSelection);

                if (course == null)
                {
                    ModelState.AddModelError(
                        nameof(model.TrainingSelection),
                        "The selected grade is currently unavailable.");

                    await ReloadPackages();
                    return View(model);
                }

                model.Grade = course.CourseName;
                model.PackageID = null;
            }

            var email = model.Email.Trim();

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.EmailAddress.ToLower() == email.ToLower());

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "An account already exists for this email address.");

                await ReloadPackages();
                return View(model);
            }

            var user = new QKZUser
            {
                FullName = $"{model.FirstName.Trim()} {model.Surname.Trim()}",
                EmailAddress = email,
                PhoneNumber = model.ContactNumber,
                RoleID = 2,
                CreatedDate = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(
                user, model.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var student = new Student
            {
                UserID = user.UserID,
                IDNumber = model.SouthAfricanId,
                Address = model.Location
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            var registration = new Registration
            {
                StudentID = student.StudentID,
                CourseID = course?.CourseID,
                PackageID = package?.PackageID,
                VenueID = null,
                RegistrationDate = DateTime.UtcNow,
                RegistrationStatus = "Pending"
            };

            _context.Registrations.Add(registration);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Success));
        }

        [HttpGet]
        public IActionResult Success()
        {
            return View();
        }
    }
}