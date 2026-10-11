using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QKZTrainingCenter.Data;
using QKZTrainingCenter.Models;
using Microsoft.AspNetCore.Hosting;
using QKZTrainingCenter.Models;

namespace QKZTrainingCenter.Controllers
{
    public class AdminController : Controller
    {
        private readonly QKZDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AdminController(
      QKZDbContext context,
      IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }
        private bool IsAdmin()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var roleId = HttpContext.Session.GetInt32("RoleID");

            return userId != null && roleId == 1;
        }

        public async Task<IActionResult> Dashboard()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.AdminName = HttpContext.Session.GetString("FullName");

            ViewBag.TotalStudents =
                await _context.Students.CountAsync();

            ViewBag.PendingRegistrations =
                await _context.Registrations
                    .CountAsync(r => r.RegistrationStatus == "Pending");

            ViewBag.TotalCourses =
                await _context.Courses.CountAsync();

            ViewBag.TotalStudyMaterials =
                await _context.StudyMaterials.CountAsync();

            return View();
        }

        public async Task<IActionResult> Students()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var students = await _context.Students
                .Join(
                    _context.Users,
                    student => student.UserID,
                    user => user.UserID,
                    (student, user) => new
                    {
                        student.StudentID,
                        user.FullName,
                        user.EmailAddress,
                        user.PhoneNumber,
                        student.IDNumber,
                        student.Address
                    })
                .OrderBy(student => student.FullName)
                .ToListAsync();

            return View(students);
        }

        public async Task<IActionResult> Registrations()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var registrations = await (
                from registration in _context.Registrations

                join student in _context.Students
                    on registration.StudentID equals student.StudentID

                join user in _context.Users
                    on student.UserID equals user.UserID

                join courseItem in _context.Courses
                    on registration.CourseID equals (int?)courseItem.CourseID
                    into courseGroup

                from course in courseGroup.DefaultIfEmpty()

                join packageItem in _context.ComboPackages
                    on registration.PackageID equals (int?)packageItem.PackageID
                    into packageGroup

                from package in packageGroup.DefaultIfEmpty()

                orderby registration.RegistrationDate descending

                select new
                {
                    registration.RegistrationID,
                    StudentName = user.FullName,
                    EmailAddress = user.EmailAddress,

                    TrainingName = course != null
                        ? course.CourseName
                        : package != null
                            ? package.PackageName
                            : "Not specified",

                    registration.RegistrationDate,
                    registration.RegistrationStatus
                }
            ).ToListAsync();

            return View(registrations);
        }

        public async Task<IActionResult> Payments()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var payments = await (
                from payment in _context.Payments
                join registration in _context.Registrations
                    on payment.RegistrationID equals registration.RegistrationID
                join student in _context.Students
                    on registration.StudentID equals student.StudentID
                join user in _context.Users
                    on student.UserID equals user.UserID
                orderby payment.PaymentDate descending
                select new
                {
                    payment.PaymentID,
                    payment.RegistrationID,
                    StudentName = user.FullName,
                    EmailAddress = user.EmailAddress,
                    payment.Amount,
                    payment.PaymentDate,
                    payment.PaymentMethod,
                    payment.TransactionReference,
                    payment.Status,
                    payment.ProofOfPayment
                }
            ).ToListAsync();

            return View(payments);

        }
public async Task<IActionResult> VerifyPayment(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentID == id);

            if (payment == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(payment.ProofOfPayment))
            {
                TempData["PaymentMessage"] =
                    "Cannot verify this payment because no proof of payment has been uploaded.";

                return RedirectToAction(nameof(Payments));
            }

            var registration = await _context.Registrations
                .FirstOrDefaultAsync(r =>
                    r.RegistrationID == payment.RegistrationID);

            if (registration == null)
            {
                return NotFound();
            }

            payment.Status = "Verified";
            registration.RegistrationStatus = "Registered Successfully";

            await _context.SaveChangesAsync();

            TempData["PaymentMessage"] =
                "Payment verified and registration updated successfully.";

            return RedirectToAction(nameof(Payments));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectPayment(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentID == id);

            if (payment == null)
            {
                return NotFound();
            }

            payment.Status = "Rejected";

            await _context.SaveChangesAsync();

            TempData["PaymentMessage"] =
                "Payment rejected. The registration has not been approved.";

            return RedirectToAction(nameof(Payments));
        }

       
// Display all courses
public async Task<IActionResult> Courses()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var courses = await _context.Courses
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            return View(courses);
        }

        // Display the Add Course form
        [HttpGet]
        public IActionResult CreateCourse()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            return View(new Course());
        }

        // Save a new course
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(Course course)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(course.CourseName))
            {
                ModelState.AddModelError(
                    nameof(course.CourseName),
                    "Course name is required.");
            }

            if (string.IsNullOrWhiteSpace(course.Title))
            {
                ModelState.AddModelError(
                    nameof(course.Title),
                    "Course title is required.");
            }

            if (!ModelState.IsValid)
            {
                return View(course);
            }

            course.CreatedDate = DateTime.UtcNow;

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            TempData["CourseMessage"] = "Course added successfully.";

            return RedirectToAction(nameof(Courses));
        }

        // Display the Edit Course form
        [HttpGet]
        public async Task<IActionResult> EditCourse(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseID == id);

            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // Save changes to an existing course
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(
            int id,
            string? courseName,
            string? title,
            string? courseDescription,
            string? courseType,
            string? duration,
            decimal? courseFee)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseID == id);

            if (course == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(courseName))
            {
                ModelState.AddModelError(
                    "CourseName",
                    "Course name is required.");
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                ModelState.AddModelError(
                    "Title",
                    "Course title is required.");
            }

            if (!ModelState.IsValid)
            {
                course.CourseName = courseName;
                course.Title = title;
                course.CourseDescription = courseDescription;
                course.CourseType = courseType;
                course.Duration = duration;
                course.CourseFee = courseFee;

                return View(course);
            }

            course.CourseName = courseName;
            course.Title = title;
            course.CourseDescription = courseDescription;
            course.CourseType = courseType;
            course.Duration = duration;
            course.CourseFee = courseFee;

            await _context.SaveChangesAsync();

            TempData["CourseMessage"] = "Course updated successfully.";

            return RedirectToAction(nameof(Courses));
        }

        
public async Task<IActionResult> Materials()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var materials = await (
                from material in _context.StudyMaterials
                join course in _context.Courses
                    on material.CourseID equals course.CourseID
                orderby material.UploadDate descending
                select new
                {
                    material.MaterialID,
                    material.Title,
                    CourseName = course.CourseName,
                    material.FileType,
                    material.FileURL,
                    material.UploadDate,
                    material.IsActive
                }
            ).ToListAsync();

            return View(materials);
        }
        
[HttpGet]
public async Task<IActionResult> CreateMaterial()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Courses = await _context.Courses
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMaterial(
            string title,
            int courseId,
            IFormFile file)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Courses = await _context.Courses
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            if (string.IsNullOrWhiteSpace(title))
            {
                ModelState.AddModelError("title", "A title is required.");
            }

            var courseExists = await _context.Courses
                .AnyAsync(c => c.CourseID == courseId);

            if (!courseExists)
            {
                ModelState.AddModelError("courseId", "Select a valid course.");
            }

            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("file", "Select a file to upload.");
            }
            else
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "file",
                        "Only PDF, DOC and DOCX files are allowed.");
                }

                if (file.Length > 10 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        "file",
                        "The file must not exceed 10 MB.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var uploadFolder = Path.Combine(
                _environment.WebRootPath,
                "materials");

            Directory.CreateDirectory(uploadFolder);

            var safeFileName = $"{Guid.NewGuid():N}{Path.GetExtension(file!.FileName).ToLowerInvariant()}";
            var filePath = Path.Combine(uploadFolder, safeFileName);

            try
            {
                await using (var stream = new FileStream(filePath, FileMode.CreateNew))
                {
                    await file.CopyToAsync(stream);
                }

                var material = new StudyMaterial
                {
                    CourseID = courseId,
                    UploadedByUserID = userId.Value,
                    Title = title.Trim(),
                    FileURL = "/materials/" + safeFileName,
                    FileType = Path.GetExtension(file.FileName).TrimStart('.').ToUpperInvariant(),
                    UploadDate = DateTime.UtcNow,
                    IsActive = true
                };

                _context.StudyMaterials.Add(material);
                await _context.SaveChangesAsync();

                TempData["MaterialMessage"] = "Study material uploaded successfully.";

                return RedirectToAction(nameof(Materials));
            }
            catch
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateMaterial(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var material = await _context.StudyMaterials
                .FirstOrDefaultAsync(m => m.MaterialID == id);

            if (material == null)
            {
                return NotFound();
            }

            material.IsActive = false;

            await _context.SaveChangesAsync();

            TempData["MaterialMessage"] = "Study material deactivated successfully.";

            return RedirectToAction(nameof(Materials));
        }
        
// Display all training schedules
public async Task<IActionResult> Schedule()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var schedules = await (
                from schedule in _context.Schedules
                join course in _context.Courses
                    on schedule.CourseID equals course.CourseID
                join mode in _context.DeliveryModes
                    on schedule.DeliveryModeID equals mode.DeliveryModeID
                orderby schedule.StartDateTime descending
                select new
                {
                    schedule.ScheduleID,
                    schedule.Title,
                    CourseName = course.CourseName,
                    DeliveryMode = mode.ModeName,
                    schedule.VenueID,
                    schedule.StartDateTime,
                    schedule.EndDateTime,
                    schedule.IsActive
                }
            ).ToListAsync();

            return View(schedules);
        }

        // Display the create schedule form
        [HttpGet]
        public async Task<IActionResult> CreateSchedule()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Courses = await _context.Courses
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            ViewBag.DeliveryModes = await _context.DeliveryModes
                .OrderBy(m => m.DeliveryModeID)
                .ToListAsync();

            return View();
        }
        
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateSchedule(
    int courseId,
    int deliveryModeId,
    string title,
    DateTime startDateTime,
    DateTime endDateTime,
    int? venueId)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Courses = await _context.Courses
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            ViewBag.DeliveryModes = await _context.DeliveryModes
                .OrderBy(m => m.DeliveryModeID)
                .ToListAsync();

            if (string.IsNullOrWhiteSpace(title))
            {
                ModelState.AddModelError(
                    "title", "A session title is required.");
            }

            if (!await _context.Courses.AnyAsync(c => c.CourseID == courseId))
            {
                ModelState.AddModelError(
                    "courseId", "Select a valid course.");
            }

            if (!await _context.DeliveryModes.AnyAsync(
                m => m.DeliveryModeID == deliveryModeId))
            {
                ModelState.AddModelError(
                    "deliveryModeId", "Select a valid delivery mode.");
            }

            if (endDateTime <= startDateTime)
            {
                ModelState.AddModelError(
                    "endDateTime", "The end time must be after the start time.");
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            var schedule = new Schedule
            {
                CourseID = courseId,
                DeliveryModeID = deliveryModeId,
                VenueID = venueId,
                Title = title.Trim(),
                StartDateTime = startDateTime,
                EndDateTime = endDateTime,
                IsActive = true
            };

            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();

            TempData["ScheduleMessage"] =
                "Training schedule created successfully.";

            return RedirectToAction(nameof(Schedule));
        }
        
// Display announcements
public async Task<IActionResult> Announcements()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var announcements = await (
                from announcement in _context.Announcements
                join courseItem in _context.Courses
                    on announcement.CourseID equals (int?)courseItem.CourseID
                    into courseGroup
                from course in courseGroup.DefaultIfEmpty()
                orderby announcement.PublishDate descending
                select new
                {
                    announcement.AnnouncementID,
                    announcement.Title,
                    announcement.Content,
                    CourseName = course != null
                        ? course.CourseName
                        : "All Courses",
                    announcement.PublishDate,
                    announcement.IsActive
                }
            ).ToListAsync();

            return View(announcements);
        }

        // Display the create announcement form
        [HttpGet]
        public async Task<IActionResult> CreateAnnouncement()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Courses = await _context.Courses
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            return View();
        }

        // Save a new announcement
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAnnouncement(
            string title,
            string content,
            int? courseId)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Courses = await _context.Courses
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            if (string.IsNullOrWhiteSpace(title))
            {
                ModelState.AddModelError("title", "Title is required.");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                ModelState.AddModelError("content", "Content is required.");
            }

            if (courseId.HasValue &&
                !await _context.Courses.AnyAsync(c => c.CourseID == courseId.Value))
            {
                ModelState.AddModelError("courseId", "Select a valid course.");
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var announcement = new Announcement
            {
                CourseID = courseId,
                AuthorUserID = userId.Value,
                Title = title.Trim(),
                Content = content.Trim(),
                PublishDate = DateTime.UtcNow,
                IsActive = true
            };

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            TempData["AnnouncementMessage"] =
                "Announcement created successfully.";

            return RedirectToAction(nameof(Announcements));
        }

        // Deactivate an announcement
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateAnnouncement(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var announcement = await _context.Announcements
                .FirstOrDefaultAsync(a => a.AnnouncementID == id);

            if (announcement == null)
            {
                return NotFound();
            }

            announcement.IsActive = false;

            await _context.SaveChangesAsync();

            TempData["AnnouncementMessage"] =
                "Announcement deactivated successfully.";

            return RedirectToAction(nameof(Announcements));
        }

    }

}
