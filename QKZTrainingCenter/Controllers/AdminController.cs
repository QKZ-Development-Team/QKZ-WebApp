using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QKZTrainingCenter.Data;
using QKZTrainingCenter.Models;
using Microsoft.AspNetCore.Hosting;

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

        // ============================================================
        // ADMIN ACCESS CHECK
        // ============================================================

        private bool IsAdmin()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var roleId = HttpContext.Session.GetInt32("RoleID");

            return userId != null && roleId == 1;
        }

        // ============================================================
        // ADMIN DASHBOARD
        // ============================================================

        public async Task<IActionResult> Dashboard()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.AdminName =
                HttpContext.Session.GetString("FullName");

            ViewBag.TotalStudents =
                await _context.Students.CountAsync();

            ViewBag.PendingRegistrations =
                await _context.Registrations
                    .CountAsync(r =>
                        r.RegistrationStatus == "Pending");

            ViewBag.TotalCourses =
                await _context.Courses.CountAsync();

            // Study Materials are temporarily not queried
            // because the StudyMaterials table is not yet
            // available in the database.
            ViewBag.TotalStudyMaterials = 0;

            return View();
        }

        // ============================================================
        // STUDENT MANAGEMENT
        // ============================================================

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

        // ============================================================
        // REGISTRATION MANAGEMENT
        // ============================================================

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

                orderby registration.RegistrationDate descending

                select new
                {
                    registration.RegistrationID,

                    StudentName = user.FullName,

                    EmailAddress = user.EmailAddress,

                    TrainingName =
                        course != null
                            ? course.CourseName
                            : "Not specified",

                    registration.RegistrationDate,

                    registration.RegistrationStatus
                }
            ).ToListAsync();

            return View(registrations);
        }

        // ============================================================
        // PAYMENT MANAGEMENT
        // ============================================================

        public async Task<IActionResult> Payments()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var payments = await (
                from payment in _context.Payments

                join registration in _context.Registrations
                    on payment.RegistrationID
                    equals registration.RegistrationID

                join student in _context.Students
                    on registration.StudentID
                    equals student.StudentID

                join user in _context.Users
                    on student.UserID
                    equals user.UserID

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

        // ============================================================
        // VERIFY PAYMENT
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> VerifyPayment(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p =>
                    p.PaymentID == id);

            if (payment == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(
                payment.ProofOfPayment))
            {
                TempData["PaymentMessage"] =
                    "Cannot verify this payment because no proof of payment has been uploaded.";

                return RedirectToAction(nameof(Payments));
            }

            var registration = await _context.Registrations
                .FirstOrDefaultAsync(r =>
                    r.RegistrationID ==
                    payment.RegistrationID);

            if (registration == null)
            {
                return NotFound();
            }

            // Only change the payment status.
            // We do NOT change RegistrationStatus because
            // the database has a CHECK constraint.

            payment.Status = "Verified";

            try
            {
                await _context.SaveChangesAsync();

                TempData["PaymentMessage"] =
                    "Payment verified successfully. The proof of payment has been checked.";
            }
            catch (DbUpdateException)
            {
                TempData["PaymentMessage"] =
                    "The payment could not be verified because of a database validation error.";
            }

            return RedirectToAction(nameof(Payments));
        }

        // ============================================================
        // REJECT PAYMENT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectPayment(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p =>
                    p.PaymentID == id);

            if (payment == null)
            {
                return NotFound();
            }

            payment.Status = "Rejected";

            try
            {
                await _context.SaveChangesAsync();

                TempData["PaymentMessage"] =
                    "Payment rejected. The registration has not been approved.";
            }
            catch (DbUpdateException)
            {
                TempData["PaymentMessage"] =
                    "The payment could not be rejected because of a database validation error.";
            }

            return RedirectToAction(nameof(Payments));
        }

        // ============================================================
        // COURSE MANAGEMENT
        // ============================================================

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

        // ============================================================
        // CREATE COURSE - GET
        // ============================================================

        [HttpGet]
        public IActionResult CreateCourse()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            return View(new Course());
        }

        // ============================================================
        // CREATE COURSE - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(
            Course course)
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

            TempData["CourseMessage"] =
                "Course added successfully.";

            return RedirectToAction(nameof(Courses));
        }

        // ============================================================
        // EDIT COURSE - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> EditCourse(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var course = await _context.Courses
                .FirstOrDefaultAsync(c =>
                    c.CourseID == id);

            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // ============================================================
        // EDIT COURSE - POST
        // ============================================================

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
                .FirstOrDefaultAsync(c =>
                    c.CourseID == id);

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
                course.CourseDescription =
                    courseDescription;
                course.CourseType =
                    courseType;
                course.Duration =
                    duration;
                course.CourseFee =
                    courseFee;

                return View(course);
            }

            course.CourseName = courseName;
            course.Title = title;
            course.CourseDescription =
                courseDescription;
            course.CourseType =
                courseType;
            course.Duration =
                duration;
            course.CourseFee =
                courseFee;

            await _context.SaveChangesAsync();

            TempData["CourseMessage"] =
                "Course updated successfully.";

            return RedirectToAction(nameof(Courses));
        }

        // ============================================================
        // TRAINING SCHEDULE
        // ============================================================

        public async Task<IActionResult> Schedule()
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var schedules = await (
                from schedule in _context.Schedules

                join course in _context.Courses
                    on schedule.CourseID
                    equals course.CourseID

                join mode in _context.DeliveryModes
                    on schedule.DeliveryModeID
                    equals mode.DeliveryModeID

                orderby schedule.StartDateTime descending

                select new
                {
                    schedule.ScheduleID,

                    schedule.Title,

                    CourseName =
                        course.CourseName,

                    DeliveryMode =
                        mode.ModeName,

                    schedule.VenueID,

                    schedule.StartDateTime,

                    schedule.EndDateTime,

                    schedule.IsActive
                }
            ).ToListAsync();

            return View(schedules);
        }

        // ============================================================
        // CREATE SCHEDULE - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> CreateSchedule()
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            ViewBag.Courses =
                await _context.Courses
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();

            ViewBag.DeliveryModes =
                await _context.DeliveryModes
                    .OrderBy(m =>
                        m.DeliveryModeID)
                    .ToListAsync();

            return View();
        }

        // ============================================================
        // CREATE SCHEDULE - POST
        // ============================================================

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
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            ViewBag.Courses =
                await _context.Courses
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();

            ViewBag.DeliveryModes =
                await _context.DeliveryModes
                    .OrderBy(m =>
                        m.DeliveryModeID)
                    .ToListAsync();

            if (string.IsNullOrWhiteSpace(title))
            {
                ModelState.AddModelError(
                    "title",
                    "A session title is required.");
            }

            if (!await _context.Courses.AnyAsync(
                c => c.CourseID == courseId))
            {
                ModelState.AddModelError(
                    "courseId",
                    "Select a valid course.");
            }

            if (!await _context.DeliveryModes.AnyAsync(
                m =>
                    m.DeliveryModeID ==
                    deliveryModeId))
            {
                ModelState.AddModelError(
                    "deliveryModeId",
                    "Select a valid delivery mode.");
            }

            if (endDateTime <= startDateTime)
            {
                ModelState.AddModelError(
                    "endDateTime",
                    "The end time must be after the start time.");
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            var schedule = new Schedule
            {
                CourseID = courseId,

                DeliveryModeID =
                    deliveryModeId,

                VenueID =
                    venueId,

                Title =
                    title.Trim(),

                StartDateTime =
                    startDateTime,

                EndDateTime =
                    endDateTime,

                IsActive = true
            };

            _context.Schedules.Add(schedule);

            await _context.SaveChangesAsync();

            TempData["ScheduleMessage"] =
                "Training schedule created successfully.";

            return RedirectToAction(
                nameof(Schedule));
        }

        // ============================================================
        // ANNOUNCEMENTS
        // ============================================================

        public async Task<IActionResult> Announcements()
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var announcements = await (
                from announcement
                    in _context.Announcements

                join courseItem in _context.Courses
                    on announcement.CourseID
                    equals (int?)courseItem.CourseID
                    into courseGroup

                from course in courseGroup
                    .DefaultIfEmpty()

                orderby announcement.PublishDate
                    descending

                select new
                {
                    announcement.AnnouncementID,

                    announcement.Title,

                    announcement.Content,

                    CourseName =
                        course != null
                            ? course.CourseName
                            : "All Courses",

                    announcement.PublishDate,

                    announcement.IsActive
                }
            ).ToListAsync();

            return View(announcements);
        }

        // ============================================================
        // CREATE ANNOUNCEMENT - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> CreateAnnouncement()
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            ViewBag.Courses =
                await _context.Courses
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();

            return View();
        }

        // ============================================================
        // CREATE ANNOUNCEMENT - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAnnouncement(
            string title,
            string content,
            int? courseId)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            ViewBag.Courses =
                await _context.Courses
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();

            if (string.IsNullOrWhiteSpace(title))
            {
                ModelState.AddModelError(
                    "title",
                    "Title is required.");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                ModelState.AddModelError(
                    "content",
                    "Content is required.");
            }

            if (courseId.HasValue &&
                !await _context.Courses.AnyAsync(
                    c =>
                        c.CourseID ==
                        courseId.Value))
            {
                ModelState.AddModelError(
                    "courseId",
                    "Select a valid course.");
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            var userId =
                HttpContext.Session.GetInt32(
                    "UserID");

            if (userId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var announcement =
                new Announcement
                {
                    CourseID =
                        courseId,

                    AuthorUserID =
                        userId.Value,

                    Title =
                        title.Trim(),

                    Content =
                        content.Trim(),

                    PublishDate =
                        DateTime.UtcNow,

                    IsActive = true
                };

            _context.Announcements.Add(
                announcement);

            await _context.SaveChangesAsync();

            TempData["AnnouncementMessage"] =
                "Announcement created successfully.";

            return RedirectToAction(
                nameof(Announcements));
        }

        // ============================================================
        // DEACTIVATE ANNOUNCEMENT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            DeactivateAnnouncement(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var announcement =
                await _context.Announcements
                    .FirstOrDefaultAsync(
                        a =>
                            a.AnnouncementID ==
                            id);

            if (announcement == null)
            {
                return NotFound();
            }

            announcement.IsActive = false;

            await _context.SaveChangesAsync();

            TempData["AnnouncementMessage"] =
                "Announcement deactivated successfully.";

            return RedirectToAction(
                nameof(Announcements));
        }
    }
}