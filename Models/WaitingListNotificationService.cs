
using eLibraryApp.Services;
using Microsoft.EntityFrameworkCore;

namespace eLibraryApp.Models
{
    public class WaitingListNotificationService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private Timer _timer;
        private readonly EmailService _emailService;

        public WaitingListNotificationService(IServiceProvider serviceProvider, EmailService emailService)
        {
            _serviceProvider = serviceProvider;
            _emailService = emailService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(CheckWaitingList, null, TimeSpan.Zero, TimeSpan.FromMinutes(10));
            return Task.CompletedTask;
        }

        private async void CheckWaitingList(object state)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
                var books = await context.Books
                    .Where(b => b.IsAvailableForBorrow && b.AvailableCopies > 0)
                    .ToListAsync();

                foreach (var book in books)
                {
                    var nextInQueue = await context.WaitingListEntries
                        .Include(w => w.User)
                        .Where(w => w.BookId == book.BookId)
                        .OrderBy(w => w.RequestedDate)
                        .FirstOrDefaultAsync();

                    if (nextInQueue != null)
                    {
                        // Send notification email
                        await _emailService.SendEmailAsync(nextInQueue.User.Email, "Your Book is Available!",
                            $"The book '{book.Title}' is now available for borrowing!");

                        // Remove from waiting list and decrement available copies
                        context.WaitingListEntries.Remove(nextInQueue);
                        book.AvailableCopies--;
                        await context.SaveChangesAsync();
                    }
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
