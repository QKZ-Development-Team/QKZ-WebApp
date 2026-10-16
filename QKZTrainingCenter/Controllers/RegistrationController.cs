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
        private readonly IWebHostEnvironment _environment;

        public RegistrationController(
            QKZDbContext context,
            IPasswordHasher<QKZUser> passwordHasher,
            IWebHostEnvironment environment)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _environment = environment;
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
            async Task ReloadPackages()
            {
                model.ComboPackages = await _context.ComboPackages
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.PackageID)
                    .ToListAsync();
            }

            // Validate ID document
            if (model.IDDocument == null || model.IDDocument.Length == 0)
            {
                ModelState.AddModelError(
                    nameof(model.IDDocument),
                    "Please upload your South African ID document.");
            }
            else
            {
                const long maxFileSize = 5 * 1024 * 1024;

                if (model.IDDocument.Length > maxFileSize)
                {
                    ModelState.AddModelError(
                        nameof(model.IDDocument),
                        "The ID document must not exceed 5 MB.");
                }

                var allowedExtensions = new[]
                {
                    ".pdf",
                    ".jpg",
                    ".jpeg",
                    ".png"
                };

                var extension = Path.GetExtension(
                    model.IDDocument.FileName)
                    .ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        nameof(model.IDDocument),
                        "Only PDF, JPG, JPEG and PNG files are allowed.");
                }
            }

            if (!ModelState.IsValid)
            {
                await ReloadPackages();
                return View(model);
            }

            // Identify whether the selection is a grade or combo package.
            Course? course = null;
            ComboPackage? package = null;

            if (model.TrainingSelection.StartsWith(
                    "Package-",
                    StringComparison.OrdinalIgnoreCase))
            {
                var packageIdText =
                    model.TrainingSelection["Package-".Length..];

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
                        p.PackageID == packageId &&
                        p.IsActive);

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
                    "Grade E",
                    "Grade D",
                    "Grade C",
                    "Grade B",
                    "Grade A"
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

                course = await _context.Courses
                    .FirstOrDefaultAsync(c =>
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

            // Check whether the email already exists.
            var email = model.Email.Trim();

            // Check whether the South African ID number already exists.
            var existingStudent = await _context.Students
                .FirstOrDefaultAsync(s =>
                    s.IDNumber == model.SouthAfricanId);

            if (existingStudent != null)
            {
                ModelState.AddModelError(
                    nameof(model.SouthAfricanId),
                    "An account already exists for this South African ID number. Please check your ID number or log in to your existing account.");

                await ReloadPackages();
                return View(model);
            }

            // Check whether the email address already exists.
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

            // Create user.
            var user = new QKZUser
            {
                FullName =
                    $"{model.FirstName.Trim()} {model.Surname.Trim()}",
                EmailAddress = email,
                PhoneNumber = model.ContactNumber,
                RoleID = 2,
                CreatedDate = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                model.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create student.
            var student = new Student
            {
                UserID = user.UserID,
                IDNumber = model.SouthAfricanId,
                Address = model.Location
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            // Create upload folder.
            var uploadFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "ids");

            Directory.CreateDirectory(uploadFolder);

            // Get the file extension.
            var fileExtension = Path.GetExtension(
                model.IDDocument!.FileName)
                .ToLowerInvariant();

            // Create a unique file name.
            var fileName =
                $"{Guid.NewGuid():N}{fileExtension}";

            var filePath = Path.Combine(
                uploadFolder,
                fileName);

            // Save the uploaded ID document.
            using (var stream = new FileStream(
                filePath,
                FileMode.Create))
            {
                await model.IDDocument.CopyToAsync(stream);
            }

            // Save the document path in the Student record.
            student.IDDocumentPath =
                $"/uploads/ids/{fileName}";

            await _context.SaveChangesAsync();

            // Create registration.
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