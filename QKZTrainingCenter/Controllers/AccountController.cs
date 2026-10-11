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

            var passwordResult = _passwordHasher.VerifyHashedPassword(
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
                    u.EmailAddress.ToLower() == email.ToLower());

            TempData["PasswordResetMessage"] =
                "If an account exists for that email address, " +
                "password reset instructions will be provided.";

            if (user != null)
            {
                var rawToken = WebEncoders.Base64UrlEncode(
                    RandomNumberGenerator.GetBytes(32));

                var tokenHash = HashToken(rawToken);
                var now = DateTime.UtcNow;

                // Invalidate previous unused reset tokens for this user.
                var previousTokens = await _context.PasswordResetTokens
                    .Where(t => t.UserID == user.UserID && !t.IsUsed)
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

                // Local testing only. Never expose reset links in production.
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

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            ViewBag.Message = TempData["PasswordResetMessage"];
            return View();
        }

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
                    u.EmailAddress.ToLower() == email.Trim().ToLower());

            if (user == null ||
                !await IsValidResetTokenAsync(user.UserID, token))
            {
                return View("ResetPasswordInvalid");
            }

            return View(new ResetPasswordViewModel
            {
                Email = user.EmailAddress,
                Token = token
            });
        }

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

            var resetToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t =>
                    t.UserID == user.UserID &&
                    t.TokenHash == tokenHash &&
                    !t.IsUsed &&
                    t.ExpiresAt > DateTime.UtcNow);

            if (resetToken == null)
            {
                return View("ResetPasswordInvalid");
            }

            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                model.NewPassword);

            resetToken.IsUsed = true;

            // Invalidate any other outstanding reset tokens.
            var otherTokens = await _context.PasswordResetTokens
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

            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            ViewBag.Message = TempData["PasswordResetSuccess"];
            return View();
        }

        private async Task<bool> IsValidResetTokenAsync(
            int userId,
            string token)
        {
            var tokenHash = HashToken(token);

            return await _context.PasswordResetTokens.AnyAsync(t =>
                t.UserID == userId &&
                t.TokenHash == tokenHash &&
                !t.IsUsed &&
                t.ExpiresAt > DateTime.UtcNow);
        }

        private static string HashToken(string token)
        {
            var tokenBytes = Encoding.UTF8.GetBytes(token);
            var hashBytes = SHA256.HashData(tokenBytes);

            return Convert.ToHexString(hashBytes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Home");
        }
    }
}