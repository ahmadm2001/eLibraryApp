using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using eLibraryApp.Models;
using Microsoft.AspNetCore.Authorization;
using eLibraryApp.Services;

namespace eLibraryApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly LibraryDbContext _context;
        private readonly EmailService _emailService;

        public AdminController(LibraryDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: Admin
        public async Task<IActionResult> Index()
        {
            return View(await _context.Books.ToListAsync());
        }

        // List all users
        public IActionResult ManageUsers()
        {
            var users = _context.Users.ToList();
            return View(users);
        }

        // Add user (GET)
        public IActionResult AddUser()
        {
            return View();
        }

        // Add user (POST)
        [HttpPost]
        public IActionResult AddUser(User user)
        {
            if (true)
            {
                _context.Users.Add(user);
                _context.SaveChanges();
                return RedirectToAction("ManageUsers");
            }
            return View(user);
        }

        // Edit user (GET)
        public IActionResult EditUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // Edit user (POST)
        [HttpPost]
        public IActionResult EditUser(User user)
        {
            if (true)
            {
                _context.Users.Update(user);
                _context.SaveChanges();
                return RedirectToAction("ManageUsers");
            }
            return View(user);
        }

        // Delete user
        public IActionResult DeleteUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
            }
            return RedirectToAction("ManageUsers");
        }

        // GET: Admin/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .FirstOrDefaultAsync(m => m.BookId == id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // GET: Admin/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BookId,CoverImageUrl ,Title,Author,Publisher,BuyPrice,BorrowPrice,YearOfPublishing,Format,AgeLimit,IsAvailableForBorrow,IsAvailableForPurchase,AvailableCopies,Genre")] Book book)
        {
            if (true)
            {
                _context.Add(book);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }

        // GET: Admin/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }
            return View(book);
        }

        // POST: Admin/Edit/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CoverImageUrl,BookId,Title,Author,Publisher,BuyPrice,BorrowPrice,YearOfPublishing,Format,AgeLimit,IsAvailableForBorrow,IsAvailableForPurchase,AvailableCopies,Genre")] Book book)
        {
            if (id != book.BookId)
            {
                return NotFound();
            }

            if (true)
            {
                try
                {
                    _context.Update(book);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(book.BookId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }

        // GET: Admin/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .FirstOrDefaultAsync(m => m.BookId == id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // POST: Admin/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                _context.Books.Remove(book);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.BookId == id);
        }
        public async Task<IActionResult> ManageBorrowedBooks()
        {
            var borrowedBooks = _context.BorrowedBooks
                .Include(bb => bb.User)
                .Include(bb => bb.Book)
                .ToList();

            return View(borrowedBooks);
        }
        public async Task<IActionResult> ManageWaitingList()
        {
            var waitingLists = _context.WaitingListEntries
                 .Include(wl => wl.User)
                 .Include(wl => wl.Book)
                 .OrderBy(wl => wl.RequestedDate)
                 .ToList();

            return View(waitingLists);
        }

        [HttpPost]
        public async Task<IActionResult> NotifyUser(int waitingListEntryId)
        {
            var entry = await _context.WaitingListEntries
                .Include(w => w.User)
                .Include(w => w.Book)
                .FirstOrDefaultAsync(w => w.WaitingListEntryId == waitingListEntryId);

            if (entry != null)
            {
                string subject = "Your Book is Now Available!";
                string message = $@"
            Dear {entry.User.FirstName},

            The book <strong>{entry.Book.Title}</strong> is now available for borrowing. Please log in to your account to borrow it within the next 24 hours.

            <a href='http://yourwebsite.com/login'>Click here to log in</a>

            If you have any questions, feel free to contact us.

            Best regards,  
            eBook Library Service
        ";

                await _emailService.SendEmailAsync(entry.User.Email, subject, message);

                // Remove user from waiting list after notification
                _context.WaitingListEntries.Remove(entry);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(ManageWaitingList));
            }

            return NotFound();
        }

        // Temporary action to test email sending
        public async Task<IActionResult> TestEmail()
        {
            try
            {
                await _emailService.SendEmailAsync("mashalm2001@gmail.com", "Test Email", "This is a test email from the eBook Library Service.");
                return Content("Email sent successfully!");
            }
            catch (Exception ex)
            {
                return Content($"Failed to send email: {ex.Message}");
            }
        }

        public async Task<IActionResult> NotifyNextUser(int bookId)
        {
            var nextInQueue = await _context.WaitingListEntries
                .Include(w => w.User)
                .Include(w => w.Book)
                .Where(w => w.BookId == bookId)
                .OrderBy(w => w.RequestedDate)
                .FirstOrDefaultAsync();

            if (nextInQueue == null)
            {
                TempData["Message"] = "No users in the waiting list for this book.";
                return RedirectToAction(nameof(ManageWaitingList));
            }

            // Notify the user via email
            await _emailService.SendEmailAsync(nextInQueue.User.Email, "Your Book is Available!",
                $"The book '{nextInQueue.Book.Title}' is now available for borrowing. Please log in to your account to borrow it.");

            // Remove the user from the waiting list
            _context.WaitingListEntries.Remove(nextInQueue);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Notification sent to {nextInQueue.User.Email}.";
            return RedirectToAction(nameof(ManageWaitingList));
        }


        [Authorize(Roles = "Admin")]
        public IActionResult AdminDashboard()
        {
            var viewModel = new AdminDashboardViewModel
            {
                TotalUsers = _context.Users.Count(),
                TotalBooks = _context.Books.Count(),
                BorrowedBooks = _context.BorrowedBooks.Count(bb => !bb.IsReturned), // Count borrowed books
                RecentUsers = _context.Users
            .OrderByDescending(u => u.UserId)
            .Take(5)
            .ToList()
            };

            return View(viewModel);
        }

        // Manage Discounts (GET)
        public IActionResult ManageDiscounts()
        {
            var books = _context.Books.ToList();
            return View(books);
        }

        // Edit Discount (GET)
        public IActionResult EditDiscount(int id)
        {
            var book = _context.Books.Find(id);
            if (book == null) return NotFound();
            return View(book);
        }

        // Edit Discount (POST)
        [HttpPost]
        public IActionResult EditDiscount(int id, decimal? discountedPrice, DateTime? discountStartDate)
        {
            var book = _context.Books.Find(id);
            if (book == null) return NotFound();

            book.DiscountedPrice = discountedPrice;
            book.DiscountStartDate = discountStartDate;

            _context.Books.Update(book);
            _context.SaveChanges();

            return RedirectToAction("ManageDiscounts");
        }

    }
}
