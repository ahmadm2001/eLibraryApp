using System.Diagnostics;
using System.Security.Claims;
using eLibraryApp.Models;
using eLibraryApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eLibraryApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly LibraryDbContext _context;
        private readonly EmailService _emailService; 
        private readonly PayPalService _payPalService;

       
        public HomeController(ILogger<HomeController> logger, LibraryDbContext context, EmailService emailService, PayPalService payPalService)
        {
            _logger = logger;
            _context = context;
            _emailService = emailService;
            _payPalService = payPalService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // Action to display the book catalog
        public IActionResult Catalog(string title, string author, string genre, decimal? price, bool? onSale, string sortOrder)
        {
            var books = _context.Books.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(title))
            {
                books = books.Where(b => b.Title.Contains(title));
            }
            if (!string.IsNullOrEmpty(author))
            {
                books = books.Where(b => b.Author.Contains(author));
            }
            if (!string.IsNullOrEmpty(genre))
            {
                books = books.Where(b => b.Genre.Contains(genre));
            }
            if (price.HasValue)
            {
                books = books.Where(b => b.BuyPrice <= price || b.BorrowPrice <= price);
            }
            if (onSale.HasValue)
            {
                books = books.Where(b => onSale.Value ? b.BuyPrice < b.BorrowPrice : b.BuyPrice >= b.BorrowPrice);
            }

            // Fetch all reviews
            var reviews = _context.Ratings
                .Select(r => new
                {
                    RatingValue = r.RatingValue,
                    Feedback = r.Feedback,
                    UserName = r.User.FirstName + " " + r.User.LastName, // Assuming you have FirstName and LastName fields
                    RatingDate = r.RatingDate
                })
                .OrderByDescending(r => r.RatingDate)
                .ToList();

            // Pass reviews to the view
            ViewBag.UserFeedbacks = reviews;

            return View(books.ToList());
        }

        // Action for BookDetails Page
        [HttpGet]
        public IActionResult BookDetails(int id)
        {
            // Fetch the book and include its ratings
            var book = _context.Books
                .Include(b => b.Ratings)
                .ThenInclude(r => r.User) // Include the user for display purposes
                .FirstOrDefault(b => b.BookId == id);

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }



        // Add to cart and redirect to the Cart page
        [HttpPost]
        public IActionResult BuyBook(int id)
        {
            var cart = GetCart();
            cart.Add(id);
            SaveCart(cart);

            TempData["Message"] = "Book added to cart successfully!";
            return RedirectToAction("Cart");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ConfirmBuyBook(int id)
        {
            var book = _context.Books.Find(id);
            if (book == null) return NotFound();

            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Login", "Account");

            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);
            if (user == null) return RedirectToAction("Login", "Account");

            var purchasedBook = new PurchasedBook
            {
                UserId = user.UserId,
                BookId = book.BookId,
                PurchaseDate = DateTime.Now
            };

            _context.PurchasedBooks.Add(purchasedBook);
            await _context.SaveChangesAsync();

            await _emailService.SendEmailAsync(user.Email, "Purchase Confirmation",
                $"You successfully purchased '{book.Title}'.");

            return RedirectToAction("Library", "Account");
        }


        [Authorize]
        public IActionResult BorrowBook(int id)
        {
            var book = _context.Books.Find(id);
            if (book == null || !book.IsAvailableForBorrow || book.AvailableCopies < 1)
            {
                TempData["Error"] = "This book is not available for borrowing.";
                return RedirectToAction("Details", new { id });
            }

            return View(book);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ConfirmBorrowBook(int id)
        {
            var book = _context.Books.Find(id);
            if (book == null || !book.IsAvailableForBorrow || book.AvailableCopies < 1)
            {
                TempData["Error"] = "This book is not available for borrowing.";
                return RedirectToAction("Details", new { id });
            }

            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);

            // Check if the user has already borrowed 3 books
            var activeBorrows = _context.BorrowedBooks.Count(bb => bb.UserId == user.UserId && !bb.IsReturned);
            if (activeBorrows >= 3)
            {
                return RedirectToAction("BorrowLimitExceeded", "Home");
            }

            var borrowDurationDays = 15; // Set borrow duration to 15 days
            var borrowedBook = new BorrowedBook
            {
                UserId = user.UserId,
                BookId = book.BookId,
                BorrowDate = DateTime.Now,
                BorrowDurationDays = borrowDurationDays
            };

            book.AvailableCopies -= 1;
            _context.BorrowedBooks.Add(borrowedBook);
            await _context.SaveChangesAsync();

            // Construct temporary download link
            var baseUrl = $"{Request.Scheme}://{Request.Host}/files/all.pdf";
            var expiryDate = DateTime.Now.AddDays(borrowDurationDays);
            var expiryInfo = $"This link will expire on {expiryDate:MMMM dd, yyyy}.";

            string subject = "Book Borrowed Successfully";
            string message = $@"
        Dear {user.FirstName},<br /><br />
        You have successfully borrowed the book <strong>{book.Title}</strong>.<br />
        You can download the PDF for the book here (valid for 15 days):<br />
        <a href='{baseUrl}' target='_blank'>Download PDF</a><br /><br />
        {expiryInfo}<br /><br />
        Thank you for using our eLibrary Service!<br /><br />
        Best regards,<br />
        eLibrary Team";

            await _emailService.SendEmailAsync(user.Email, subject, message);

            TempData["Success"] = "Book borrowed successfully! A confirmation email has been sent to your email address.";
            return RedirectToAction("Library", "Account");
        }



        public IActionResult BorrowLimitExceeded()
        {
            return View();
        }

        public IActionResult AddToWaitingList(int id)
        {
            var book = _context.Books.Find(id);
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);

            if (book == null || user == null)
            {
                return NotFound();
            }

            var waitingListEntry = new WaitingListEntry
            {
                UserId = user.UserId,
                BookId = id,
                RequestedDate = DateTime.Now
            };

            _context.WaitingListEntries.Add(waitingListEntry);
            _context.SaveChanges();

            TempData["Success"] = "You have been added to the waiting list. You will be notified when the book is available.";
            return RedirectToAction("Details", new { id });
        }

        public IActionResult AddToCart(int id)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<int>>("Cart") ?? new List<int>();
            cart.Add(id);
            HttpContext.Session.SetObjectAsJson("Cart", cart);
            return RedirectToAction("Cart");
        }

        public IActionResult Cart()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<int>>("Cart") ?? new List<int>();
            var booksInCart = _context.Books.Where(b => cart.Contains(b.BookId)).ToList();
            return View(booksInCart);
        }

        public IActionResult RemoveFromCart(int id)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<int>>("Cart") ?? new List<int>();
            cart.Remove(id);
            HttpContext.Session.SetObjectAsJson("Cart", cart);
            return RedirectToAction("Cart");
        }

        [HttpGet]
        public IActionResult GetCartItemCount()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<int>>("Cart") ?? new List<int>();
            return Json(cart.Count);
        }

        [HttpGet]
        [Authorize]
        public IActionResult Checkout()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<int>>("Cart") ?? new List<int>();
            var booksInCart = _context.Books.Where(b => cart.Contains(b.BookId)).ToList();
            return View(booksInCart);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ProcessPayment(decimal totalAmount)
        {
            var approvalLink = await _payPalService.CreateOrder(totalAmount);
            return Redirect(approvalLink);
        }

        [HttpGet]
        public IActionResult PaymentSuccess(string token)
        {
            ViewBag.Message = "Payment Successful!";
            return View("Success");
        }

        [HttpGet]
        public IActionResult PaymentCancel()
        {
            ViewBag.Message = "Payment was canceled.";
            return View("Cancel");
        }

        [HttpPost]
        public IActionResult ClearCart()
        {
            HttpContext.Session.Remove("Cart");
            return Ok();
        }

        // Method to fetch the cart from session or create a new one
        private List<int> GetCart()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<int>>("Cart") ?? new List<int>();
            return cart;
        }

        // Save cart back to session
        private void SaveCart(List<int> cart)
        {
            HttpContext.Session.SetObjectAsJson("Cart", cart);
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CompleteOrder()
        {
            // Get the user's email from claims
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            // Find the user in the database
            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Retrieve the cart items from the session
            var cart = HttpContext.Session.GetObjectFromJson<List<int>>("Cart") ?? new List<int>();

            // Keep track of the books added to the library
            var addedBooks = new List<Book>();

            foreach (var bookId in cart)
            {
                var book = _context.Books.FirstOrDefault(b => b.BookId == bookId);
                if (book != null)
                {
                    // Add the book to PurchasedBooks
                    var purchasedBook = new PurchasedBook
                    {
                        UserId = user.UserId,
                        BookId = book.BookId,
                        PurchaseDate = DateTime.Now
                    };
                    _context.PurchasedBooks.Add(purchasedBook);
                    addedBooks.Add(book); // Track added books
                }
            }

            // Save changes to the database
            await _context.SaveChangesAsync();

            // Clear the cart
            HttpContext.Session.Remove("Cart");

            // Send email notification
            if (addedBooks.Any())
            {
                // Single download link for all books
                var downloadUrl = $"{Request.Scheme}://{Request.Host}/files/all.pdf";

                var bookTitles = string.Join(", ", addedBooks.Select(book => $"<strong>{book.Title}</strong>"));
                string subject = "Books Added to Your Library";
                string message = $@"
            Dear {user.FirstName},<br /><br />
            The following books have been successfully added to your library:<br />
            {bookTitles}<br /><br />
            You can download the PDF file here:<br />
            <a href='{downloadUrl}' target='_blank'>Download PDF</a><br /><br />
            Thank you for using our eLibrary Service!<br /><br />
            Best regards,<br />
            eLibrary Team";

                await _emailService.SendEmailAsync(user.Email, subject, message);
            }

            // Redirect to the success page
            return RedirectToAction("PaymentSuccess", "Home");
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ReturnBook(int id)
        {
            var borrowedBook = _context.BorrowedBooks
                .Include(bb => bb.Book)
                .FirstOrDefault(bb => bb.BorrowedBookId == id && !bb.IsReturned);

            if (borrowedBook == null)
            {
                TempData["Error"] = "The book was not found or has already been returned.";
                return RedirectToAction("Library", "Account");
            }

            // Mark the book as returned
            borrowedBook.IsReturned = true;

            // Increase the available copies of the book
            if (borrowedBook.Book != null)
            {
                borrowedBook.Book.AvailableCopies += 1;
            }

            // Save changes to the database
            await _context.SaveChangesAsync();

            TempData["Success"] = "Book returned successfully!";
            return RedirectToAction("Library", "Account");
        }



        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ConfirmReturnBook(int id)
        {
            var borrowedBook = _context.BorrowedBooks
                .Include(bb => bb.Book)
                .FirstOrDefault(bb => bb.BorrowedBookId == id && !bb.IsReturned);

            if (borrowedBook == null)
            {
                TempData["Error"] = "The book was not found or has already been returned.";
                return RedirectToAction("Library", "Account");
            }

            // Mark the book as returned
            borrowedBook.IsReturned = true;

            // Increase the available copies of the book
            if (borrowedBook.Book != null)
            {
                borrowedBook.Book.AvailableCopies += 1;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Book returned successfully!";
            return RedirectToAction("Library", "Account");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SubmitRating(int bookId, int ratingValue, string feedback)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "You must be logged in to leave a review." });
            }

            // Create and save the rating
            var rating = new Rating
            {
                UserId = int.Parse(userId),
                BookId = bookId,
                RatingValue = ratingValue,
                Feedback = feedback,
                RatingDate = DateTime.Now
            };

            _context.Ratings.Add(rating);
            await _context.SaveChangesAsync();

            // Fetch user details for display
            var user = _context.Users.FirstOrDefault(u => u.UserId == rating.UserId);

            return Json(new
            {
                success = true,
                ratingValue = rating.RatingValue,
                feedback = rating.Feedback,
                userName = user?.FirstName ?? "Anonymous",
                date = rating.RatingDate.ToString("MM/dd/yyyy")
            });
        }



    }
}
