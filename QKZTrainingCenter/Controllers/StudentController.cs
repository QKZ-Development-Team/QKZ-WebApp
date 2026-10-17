using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QKZTrainingCenter.Data;
using QKZTrainingCenter.Models;

namespace QKZTrainingCenter.Controllers
{
    public class StudentController : Controller
    {
        private readonly QKZDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public StudentController(
            QKZDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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

            if (userId == null || roleId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (roleId == 1)
            {
                return RedirectToAction("Dashboard", "Admin");
            }

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

            // Get all active registrations for this student
            var registrations = await _context.Registrations
                .Where(r => r.StudentID == student.StudentID
                            && r.RegistrationStatus != "Cancelled")
                .ToListAsync();

            // Courses registered individually
            var courseIds = registrations
                .Where(r => r.CourseID != null)
                .Select(r => r.CourseID!.Value)
                .ToList();

            // Combo packages registered by the student
            var packageIds = registrations
                .Where(r => r.PackageID != null)
                .Select(r => r.PackageID!.Value)
                .Distinct()
                .ToList();

            // Add courses that belong to the student's combo packages
            if (packageIds.Any())
            {
                var packageCourseIds = await _context.ComboPackageCourses
                    .Where(pc => packageIds.Contains(pc.PackageID))
                    .Select(pc => pc.CourseID)
                    .ToListAsync();

                courseIds.AddRange(packageCourseIds);
            }

            // Remove duplicate courses
            courseIds = courseIds
                .Distinct()
                .ToList();

            // Get active study materials for all registered courses
            var materials = await _context.StudyMaterials
                .Where(m => courseIds.Contains(m.CourseID)
                            && m.IsActive)
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


        [HttpGet]
        public async Task<IActionResult> UploadID()
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
                return NotFound();
            }

            ViewBag.IDUploaded =
                !string.IsNullOrEmpty(student.IDDocumentPath);

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadID(IFormFile idDocument)
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (idDocument == null || idDocument.Length == 0)
            {
                ModelState.AddModelError(
                    "idDocument",
                    "Please select your ID document.");

                ViewBag.IDUploaded = false;
                return View();
            }

            const long maxFileSize = 5 * 1024 * 1024;

            if (idDocument.Length > maxFileSize)
            {
                ModelState.AddModelError(
                    "idDocument",
                    "The ID document must not exceed 5 MB.");

                ViewBag.IDUploaded = false;
                return View();
            }

            var allowedExtensions = new[]
            {
                ".pdf",
                ".jpg",
                ".jpeg",
                ".png"
            };

            var extension = Path.GetExtension(idDocument.FileName)
                .ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(
                    "idDocument",
                    "Only PDF, JPG, JPEG and PNG files are allowed.");

                ViewBag.IDUploaded = false;
                return View();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserID == userId.Value);

            if (student == null)
            {
                return NotFound();
            }

            var uploadFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "ids");

            Directory.CreateDirectory(uploadFolder);

            var fileName = $"{Guid.NewGuid():N}{extension}";

            var filePath = Path.Combine(uploadFolder, fileName);

            using (var stream = new FileStream(
                filePath,
                FileMode.Create))
            {
                await idDocument.CopyToAsync(stream);
            }

            if (!string.IsNullOrEmpty(student.IDDocumentPath))
            {
                var oldFilePath = Path.Combine(
                    _environment.WebRootPath,
                    student.IDDocumentPath.TrimStart('/')
                        .Replace(
                            '/',
                            Path.DirectorySeparatorChar));

                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }

            student.IDDocumentPath =
                $"/uploads/ids/{fileName}";

            await _context.SaveChangesAsync();

            TempData["IDUploadMessage"] =
                "Your ID document has been uploaded successfully.";

            return RedirectToAction(nameof(UploadID));
        }


        // ============================================================
        // MY DOCUMENTS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Documents()
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
                return RedirectToAction("Dashboard", "Student");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserID == userId.Value);

            var registration = await _context.Registrations
                .Where(r => r.StudentID == student.StudentID)
                .OrderByDescending(r => r.RegistrationDate)
                .FirstOrDefaultAsync();

            Payment? payment = null;

            if (registration != null)
            {
                payment = await _context.Payments
                    .FirstOrDefaultAsync(p =>
                        p.RegistrationID ==
                        registration.RegistrationID);
            }

            var model = new StudentDocumentsViewModel
            {
                FullName = user?.FullName ?? string.Empty,
                IDDocumentPath = student.IDDocumentPath,
                ProofOfPaymentPath = payment?.ProofOfPayment,
                PaymentStatus =
                    payment?.Status ?? "Not Submitted"
            };

            return View(model);
        }


        // ============================================================
        // UPLOAD PROOF OF PAYMENT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProofOfPayment(
            IFormFile proofOfPayment,
            decimal amount,
            string transactionReference)
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
                return RedirectToAction("Dashboard", "Student");
            }

            var registration = await _context.Registrations
                .Where(r => r.StudentID == student.StudentID)
                .OrderByDescending(r => r.RegistrationDate)
                .FirstOrDefaultAsync();

            if (registration == null)
            {
                TempData["DocumentError"] =
                    "No registration was found for your account.";

                return RedirectToAction(nameof(Documents));
            }

            if (proofOfPayment == null ||
                proofOfPayment.Length == 0)
            {
                TempData["DocumentError"] =
                    "Please select your proof of payment.";

                return RedirectToAction(nameof(Documents));
            }

            if (amount <= 0)
            {
                TempData["DocumentError"] =
                    "Please enter a valid payment amount.";

                return RedirectToAction(nameof(Documents));
            }

            if (string.IsNullOrWhiteSpace(transactionReference))
            {
                TempData["DocumentError"] =
                    "Please enter your payment reference.";

                return RedirectToAction(nameof(Documents));
            }

            const long maxFileSize = 5 * 1024 * 1024;

            if (proofOfPayment.Length > maxFileSize)
            {
                TempData["DocumentError"] =
                    "The proof of payment file must not exceed 5 MB.";

                return RedirectToAction(nameof(Documents));
            }

            var proofExtension =
                Path.GetExtension(proofOfPayment.FileName)
                    .ToLowerInvariant();

            var allowedExtensions = new[]
            {
                ".pdf",
                ".jpg",
                ".jpeg",
                ".png"
            };

            if (!allowedExtensions.Contains(proofExtension))
            {
                TempData["DocumentError"] =
                    "Only PDF, JPG, JPEG and PNG files are allowed.";

                return RedirectToAction(nameof(Documents));
            }

            var uploadFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "payments");

            Directory.CreateDirectory(uploadFolder);

            var fileName =
                $"{Guid.NewGuid():N}{proofExtension}";

            var filePath =
                Path.Combine(uploadFolder, fileName);

            using (var stream = new FileStream(
                filePath,
                FileMode.Create))
            {
                await proofOfPayment.CopyToAsync(stream);
            }

            var relativePath =
                $"/uploads/payments/{fileName}";

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p =>
                    p.RegistrationID ==
                    registration.RegistrationID);

            if (payment == null)
            {
                payment = new Payment
                {
                    RegistrationID =
                        registration.RegistrationID,

                    Amount = amount,

                    PaymentDate = DateTime.UtcNow,

                    PaymentMethod = "Bank Transfer",

                    TransactionReference =
                        transactionReference.Trim(),

                    Status = "Pending Verification",

                    ProofOfPayment = relativePath
                };

                _context.Payments.Add(payment);
            }
            else
            {
                payment.Amount = amount;

                payment.PaymentDate = DateTime.UtcNow;

                payment.PaymentMethod = "Bank Transfer";

                payment.TransactionReference =
                    transactionReference.Trim();

                payment.Status = "Pending Verification";

                payment.ProofOfPayment = relativePath;
            }

            await _context.SaveChangesAsync();

            TempData["DocumentSuccess"] =
                "Proof of payment uploaded successfully. Your payment is pending verification.";

            return RedirectToAction(nameof(Documents));
        }
    }
}