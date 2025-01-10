using System.ComponentModel.DataAnnotations;

namespace eLibraryApp.Models
{
    public class Book
    {
        [Key]
        public int BookId { get; set; }

        [Required, MaxLength(100)]
        public string Title { get; set; }

        [Required, MaxLength(50)]
        public string Author { get; set; }

        [MaxLength(50)]
        public string Publisher { get; set; }

        [Required, Range(1, int.MaxValue)]
        public decimal BuyPrice { get; set; }

        [Range(1, int.MaxValue)]
        public decimal? BorrowPrice { get; set; }

        [Required]
        public int YearOfPublishing { get; set; }

        [Required]
        public string Format { get; set; } // EPUB, FB2, MOBI, PDF

        public int AgeLimit { get; set; } // e.g., 18+, 8+

        public bool IsAvailableForBorrow { get; set; }
        public bool IsAvailableForPurchase { get; set; }

        public int? AvailableCopies { get; set; }
        [Required]
        public string Genre { get; set; } // New column for Genre

        [Required]
        public string CoverImageUrl { get; set; } // New column for Cover Image URL
        public List<Rating> Ratings { get; set; }

        public decimal? DiscountedPrice { get; set; }
        public DateTime? DiscountStartDate { get; set; } // Start date of the discount
        // Check if the discount is valid (only for a week)
        public bool IsDiscountActive =>
            DiscountStartDate.HasValue && DiscountStartDate.Value.AddDays(7) >= DateTime.Now;

        // Calculate remaining discount time in days
        public string DiscountRemainingTime
        {
            get
            {
                if (!IsDiscountActive) return null;
                var remainingTime = DiscountStartDate.Value.AddDays(7) - DateTime.Now;
                return remainingTime.Days > 0
                    ? $"{remainingTime.Days} days left"
                    : $"{remainingTime.Hours} hours left";
            }
        }

        public string? PdfDownloadUrl { get; set; }
    }
}
