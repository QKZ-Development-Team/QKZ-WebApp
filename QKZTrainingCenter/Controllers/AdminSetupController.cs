using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QKZTrainingCenter.Data;
using QKZTrainingCenter.Models;

namespace QKZTrainingCenter.Controllers
{
    public class AdminSetupController : Controller
    {
        private readonly QKZDbContext _context;
        private readonly IPasswordHasher<QKZUser> _passwordHasher;
        private readonly IWebHostEnvironment _environment;

        public AdminSetupController(
            QKZDbContext context,
            IPasswordHasher<QKZUser> passwordHasher,
            IWebHostEnvironment environment)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(AdminSetupViewModel model)
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.EmailAddress == model.EmailAddress);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "EmailAddress",
                    "A user with this email address already exists.");

                return View(model);
            }

            var admin = new QKZUser
            {
                FullName = model.FullName,
                EmailAddress = model.EmailAddress,
                PhoneNumber = model.PhoneNumber,
                RoleID = 1,
                CreatedDate = DateTime.UtcNow
            };

            admin.PasswordHash = _passwordHasher.HashPassword(
                admin,
                model.Password);

            _context.Users.Add(admin);

            await _context.SaveChangesAsync();

            return RedirectToAction("Success");
        }

        [HttpGet]
        public IActionResult Success()
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            return View();
        }
    }
}