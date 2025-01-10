using eLibraryApp.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text;
using eLibraryApp.Services;

namespace eLibraryApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly LibraryDbContext _context;
        private readonly EmailService _emailService;
        public AccountController(LibraryDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // Login (GET)
        public IActionResult Login()
        {
            return View();
        }

        // Login (POST)
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user != null)
            {
                var hashedPassword = HashPassword(password, user.PasswordSalt);
                if (hashedPassword == user.Password)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim(ClaimTypes.Name, user.FirstName),
                        new Claim(ClaimTypes.Role, user.Role),
                        new Claim(ClaimTypes.Email, user.Email) // Add email claim
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true // Remember Me functionality
                    };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                    await _emailService.SendEmailAsync(user.Email, "Login Notification",
                        $"Dear {user.FirstName},<br />You have successfully logged into your account.");

                    return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.ErrorMessage = "Invalid email or password.";
            return View();
        }



        // Signup (GET)
        [HttpGet]
        public IActionResult Signup()
        {
            // Check if an admin exists in the database
            bool adminExists = _context.Users.Any(u => u.Role == "Admin");
            ViewBag.AdminExists = adminExists;

            return View();
        }


        // Signup (POST)
        [HttpPost]
        public async Task<IActionResult> Signup(User user)
        {
            if (_context.Users.Any(u => u.Role == "Admin"))
            {
                user.Role = "User"; // Force role to User if an admin already exists
            }
            if (true)
            {
                // Generate a PasswordSalt
                var salt = GenerateSalt();
                user.PasswordSalt = salt;

                // Hash the password with the salt
                user.Password = HashPassword(user.Password, salt);

                // Save the user to the database
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Send signup confirmation email
                await _emailService.SendEmailAsync(user.Email, "Signup Confirmation",
                    $"Dear {user.FirstName},<br />Welcome to eBook Library Service! Your account has been successfully created.");

                return RedirectToAction("Login");
            }

            return View(user);
        }

        private string GenerateSalt()
        {
            var rng = new RNGCryptoServiceProvider();
            var saltBytes = new byte[16];
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        private string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var combinedPasswordSalt = password + salt;
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedPasswordSalt));
                return Convert.ToBase64String(hashBytes);
            }
        }

        // Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // Access Denied
        public IActionResult AccessDenied()
        {
            return View();
        }

        // User Profile (GET)
        [Authorize]
        [HttpGet]
        public IActionResult UserProfile()
        {
            var userId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userId, out var id))
            {
                var user = _context.Users.FirstOrDefault(u => u.UserId == id);

                if (user != null)
                {
                    return View(user);
                }
            }

            return RedirectToAction("Login");
        }

        // Update Profile (POST)
        [HttpPost]
        [Authorize]
        public IActionResult UpdateProfile(User user)
        {
            if (true)
            {
                var existingUser = _context.Users.Find(user.UserId);
                if (existingUser != null)
                {
                    existingUser.FirstName = user.FirstName;
                    existingUser.LastName = user.LastName;
                    existingUser.Email = user.Email;

                    // Update only if the password is being changed
                    if (!string.IsNullOrEmpty(user.Password))
                    {
                        string salt;
                        existingUser.Password = HashPassword(user.Password, out salt);
                        existingUser.PasswordSalt = salt;
                    }

                    _context.SaveChanges();
                    ViewBag.SuccessMessage = "Profile updated successfully!";
                }
            }
            return View("UserProfile", user);
        }

        // Forgot Password (GET)
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // Forgot Password (POST)
        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                ViewBag.ErrorMessage = "Email not found.";
                return View();
            }

            // Send reset password email logic (placeholder)
            ViewBag.SuccessMessage = "Password reset link sent to your email.";
            return View();
        }

        // Utility to hash passwords
        private string HashPassword(string password, out string salt)
        {
            byte[] saltBytes = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            salt = Convert.ToBase64String(saltBytes);

            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: saltBytes,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            return hashed;
        }

        // Utility to verify password
        private bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            byte[] saltBytes = Convert.FromBase64String(storedSalt);

            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: saltBytes,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            return hashed == storedHash;
        }

        [Authorize]
        [HttpGet]
        public IActionResult Library()
        {
            var userId = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userId, out var id))
            {
                var user = _context.Users
                    .Include(u => u.BorrowedBooks.Where(bb => !bb.IsReturned)) // Exclude returned books
                        .ThenInclude(bb => bb.Book)
                    .Include(u => u.PurchasedBooks)
                        .ThenInclude(pb => pb.Book)
                    .FirstOrDefault(u => u.UserId == id);

                if (user != null)
                {
                    return View(user);
                }
            }

            return RedirectToAction("Login");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SendDownloadLink(int id)
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var purchasedBook = _context.PurchasedBooks
                .Include(pb => pb.Book)
                .FirstOrDefault(pb => pb.BookId == id && pb.User.Email == userEmail);

            if (purchasedBook == null)
            {
                TempData["Error"] = "The requested book was not found in your purchased books.";
                return RedirectToAction("Library", "Account");
            }

            // Construct the email message
            string subject = $"Your Download Link for {purchasedBook.Book.Title}";
            string message = $@"
        <p>Dear {User.FindFirst(ClaimTypes.Name)?.Value},</p>
        <p>Thank you for purchasing <strong>{purchasedBook.Book.Title}</strong>. Please find your download link below:</p>
        <p><a href='{Request.Scheme}://{Request.Host}/files/all.pdf' target='_blank'>Download PDF</a></p>
        <p>We hope you enjoy reading!</p>
        <p>Best regards,<br>eBook Library Team</p>";

            // Send the email with the PDF attachment
            var attachmentPath = Path.Combine("wwwroot", "files", "all.pdf");
            await _emailService.SendEmailAsync(userEmail, subject, message, new List<string> { attachmentPath });

            // Redirect to the confirmation page
            return RedirectToAction("DownloadConfirmation", "Account", new { bookTitle = purchasedBook.Book.Title });
        }

        [HttpGet]
        [Authorize]
        public IActionResult DownloadConfirmation(string bookTitle)
        {
            ViewBag.BookTitle = bookTitle;
            return View();
        }

    }
}
