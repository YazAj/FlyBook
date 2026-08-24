using FlyBook.Data;
using FlyBook.Models;
using FlyBook.Services;
using FlyBook.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlyBook.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new RegisterViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim();
            var normalizedEmail = email.ToLower();

            var exists = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == normalizedEmail);

            if (exists)
            {
                ModelState.AddModelError(nameof(model.Email), "This email is already registered.");

                return View(model);
            }

            var user = new User
            {
                FullName = model.FullName.Trim(),
                Email = email,
                Password = PasswordHelper.HashPassword(model.Password),
                Role = "User",
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public async Task<IActionResult> Login()
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");

            if (sessionUserId != null)
            {
                if (HttpContext.Session.GetString("Role") == "User")
                {
                    var hasActiveSession = await _context.Users
                        .AnyAsync(u => u.UserId == sessionUserId.Value && u.Role == "User" && u.IsActive);

                    if (!hasActiveSession)
                    {
                        HttpContext.Session.Clear();
                        ModelState.AddModelError(string.Empty, GetDisabledAccountMessage());

                        return View(new LoginViewModel());
                    }
                }

                return RedirectToAction("Index", "Home");
            }

            if (TempData["LoginError"] is string loginError)
            {
                ModelState.AddModelError(string.Empty, loginError);
            }

            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim().ToLower();
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            var result = PasswordHelper.VerifyPassword(user, model.Password);

            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            if (user.Role == "Admin")
            {
                ModelState.AddModelError(string.Empty, "Please use the Admin Login page.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, GetDisabledAccountMessage());
                return View(model);
            }

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.Password = PasswordHelper.HashPassword(model.Password);
                await _context.SaveChangesAsync();
            }

            SignIn(user);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);

            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction(nameof(Login));
            }

            if (!user.IsActive)
            {
                HttpContext.Session.Clear();
                TempData["LoginError"] = GetDisabledAccountMessage();
                return RedirectToAction(nameof(Login));
            }

            return View(user);
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";

            return RedirectToAction(nameof(Login));
        }

        private void SignIn(User user)
        {
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserName", user.FullName);
            HttpContext.Session.SetString("Role", user.Role);
            HttpContext.Session.SetString("UserRole", user.Role);
        }

        private static string GetDisabledAccountMessage()
        {
            return "Unable to sign in. Your account is currently disabled. Please contact the administrator.";
        }
    }
}
