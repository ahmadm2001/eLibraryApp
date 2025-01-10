using System.ComponentModel.DataAnnotations;

namespace eLibraryApp.Models
{
    public class BorrowedBook
    {
        [Key]
        public int BorrowedBookId { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        public int BookId { get; set; }
        public Book Book { get; set; }

        [Required]
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate => BorrowDate.AddDays(BorrowDurationDays);

        public bool IsReturned { get; set; }

        public int BorrowDurationDays { get; set; } = 14; // Default borrow duration: 14 days
    }
}
