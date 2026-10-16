using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using QKZTrainingCenter.Data;
using QKZTrainingCenter.Models;

namespace QKZTrainingCenter.Controllers
{
    public class AccountController : Controller
    {
        private readonly QKZDbContext _context;
        private readonly IPasswordHasher<QKZUser> _passwordHasher;
        private readonly IWebHostEnvironment _environment;

        public AccountController(
            QKZDbContext context,
            IPasswordHasher<QKZUser> passwordHasher,
            IWebHostEnvironment environment)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _environment = environment;
        }

        // =========================================================
        // STUDENT LOGIN
        // =========================================================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(StudentLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim();

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.EmailAddress.ToLower() == email.ToLower());

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid email address or password.");

                return View(model);
            }

            var passwordResult =
                _passwordHasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    model.Password);

            if (passwordResult == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid email address or password.");

                return View(model);
            }

            HttpContext.Session.SetInt32("UserID", user.UserID);
            HttpContext.Session.SetInt32("RoleID", user.RoleID);
            HttpContext.Session.SetString("FullName", user.FullName);

            if (user.RoleID == 1)
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            if (user.RoleID == 2)
            {
                return RedirectToAction("Dashboard", "Student");
            }

            HttpContext.Session.Clear();

            return Forbid();
        }


        // =========================================================
        // ADMIN LOGIN
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> AdminLogin()
        {
            // Make sure the test administrator account exists.
            await EnsureAdminAccountAsync();

            return View(new StudentLoginViewModel
            {
                Email = "admin@qkztraining.co.za"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogin(
            StudentLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim();

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.EmailAddress.ToLower() == email.ToLower());

            // Account must exist and must have Administrator role.
            if (user == null || user.RoleID != 1)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid administrator email address or password.");

                return View(model);
            }

            var passwordResult =
                _passwordHasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    model.Password);

            if (passwordResult == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid administrator email address or password.");

                return View(model);
            }

            HttpContext.Session.SetInt32("UserID", user.UserID);
            HttpContext.Session.SetInt32("RoleID", user.RoleID);
            HttpContext.Session.SetString("FullName", user.FullName);

            return RedirectToAction("Dashboard", "Admin");
        }


        // =========================================================
        // CREATE TEST ADMIN ACCOUNT
        // =========================================================

        private async Task EnsureAdminAccountAsync()
        {
            const string adminEmail = "admin@qkztraining.co.za";
            const string adminPassword = "Admin@123";
            const string adminName = "QKZ Administrator";

            var admin = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.EmailAddress.ToLower() ==
                    adminEmail.ToLower());

            // If admin does not exist, create the account.
            if (admin == null)
            {
                admin = new QKZUser
                {
                    FullName = adminName,
                    EmailAddress = adminEmail,
                    RoleID = 1
                };

                admin.PasswordHash =
                    _passwordHasher.HashPassword(
                        admin,
                        adminPassword);

                _context.Users.Add(admin);

                await _context.SaveChangesAsync();

                return;
            }

            // If the account exists but is not an administrator,
            // correct the role and reset the test password.
            if (admin.RoleID != 1)
            {
                admin.RoleID = 1;

                admin.PasswordHash =
                    _passwordHasher.HashPassword(
                        admin,
                        adminPassword);

                await _context.SaveChangesAsync();
            }
        }


        // =========================================================
        // FORGOT PASSWORD
        // =========================================================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim();

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.EmailAddress.ToLower() ==
                    email.ToLower());

            TempData["PasswordResetMessage"] =
                "If an account exists for that email address, " +
                "password reset instructions will be provided.";

            if (user != null)
            {
                var rawToken =
                    WebEncoders.Base64UrlEncode(
                        RandomNumberGenerator.GetBytes(32));

                var tokenHash = HashToken(rawToken);

                var now = DateTime.UtcNow;

                // Invalidate previous unused reset tokens.
                var previousTokens =
                    await _context.PasswordResetTokens
                        .Where(t =>
                            t.UserID == user.UserID &&
                            !t.IsUsed)
                        .ToListAsync();

                foreach (var previousToken in previousTokens)
                {
                    previousToken.IsUsed = true;
                }

                var resetToken = new PasswordResetToken
                {
                    UserID = user.UserID,
                    TokenHash = tokenHash,
                    CreatedAt = now,
                    ExpiresAt = now.AddMinutes(30),
                    IsUsed = false
                };

                _context.PasswordResetTokens.Add(resetToken);

                await _context.SaveChangesAsync();

                // Development/testing only.
                if (_environment.IsDevelopment())
                {
                    var resetLink = Url.Action(
                        nameof(ResetPassword),
                        "Account",
                        new
                        {
                            email = user.EmailAddress,
                            token = rawToken
                        },
                        Request.Scheme);

                    ViewBag.DevResetLink = resetLink;
                }
            }

            return View("ForgotPasswordConfirmation");
        }


        // =========================================================
        // FORGOT PASSWORD CONFIRMATION
        // =========================================================

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            ViewBag.Message =
                TempData["PasswordResetMessage"];

            return View();
        }


        // =========================================================
        // RESET PASSWORD
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ResetPassword(
            string email,
            string token)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(token))
            {
                return View("ResetPasswordInvalid");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.EmailAddress.ToLower() ==
                    email.Trim().ToLower());

            if (user == null ||
                !await IsValidResetTokenAsync(
                    user.UserID,
                    token))
            {
                return View("ResetPasswordInvalid");
            }

            return View(new ResetPasswordViewModel
            {
                Email = user.EmailAddress,
                Token = token
            });
        }


        // =========================================================
        // RESET PASSWORD - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.EmailAddress.ToLower() ==
                    model.Email.Trim().ToLower());

            if (user == null)
            {
                return View("ResetPasswordInvalid");
            }

            var tokenHash = HashToken(model.Token);

            var resetToken =
                await _context.PasswordResetTokens
                    .FirstOrDefaultAsync(t =>
                        t.UserID == user.UserID &&
                        t.TokenHash == tokenHash &&
                        !t.IsUsed &&
                        t.ExpiresAt > DateTime.UtcNow);

            if (resetToken == null)
            {
                return View("ResetPasswordInvalid");
            }

            user.PasswordHash =
                _passwordHasher.HashPassword(
                    user,
                    model.NewPassword);

            resetToken.IsUsed = true;

            // Invalidate other outstanding reset tokens.
            var otherTokens =
                await _context.PasswordResetTokens
                    .Where(t =>
                        t.UserID == user.UserID &&
                        t.PasswordResetTokenID !=
                        resetToken.PasswordResetTokenID &&
                        !t.IsUsed)
                    .ToListAsync();

            foreach (var otherToken in otherTokens)
            {
                otherToken.IsUsed = true;
            }

            await _context.SaveChangesAsync();

            TempData["PasswordResetSuccess"] =
                "Your password has been reset successfully. " +
                "You can now log in using your new password.";

            return RedirectToAction(
                nameof(ResetPasswordConfirmation));
        }


        // =========================================================
        // RESET PASSWORD CONFIRMATION
        // =========================================================

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            ViewBag.Message =
                TempData["PasswordResetSuccess"];

            return View();
        }


        // =========================================================
        // CHECK RESET TOKEN
        // =========================================================

        private async Task<bool> IsValidResetTokenAsync(
            int userId,
            string token)
        {
            var tokenHash = HashToken(token);

            return await _context.PasswordResetTokens
                .AnyAsync(t =>
                    t.UserID == userId &&
                    t.TokenHash == tokenHash &&
                    !t.IsUsed &&
                    t.ExpiresAt > DateTime.UtcNow);
        }


        // =========================================================
        // HASH RESET TOKEN
        // =========================================================

        private static string HashToken(string token)
        {
            var tokenBytes =
                Encoding.UTF8.GetBytes(token);

            var hashBytes =
                SHA256.HashData(tokenBytes);

            return Convert.ToHexString(hashBytes);
        }


        // =========================================================
        // LOGOUT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction(
                "Index",
                "Home");
        }
    }
}