using System.ComponentModel.DataAnnotations;

namespace eLibraryApp.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required, MaxLength(50)]
        public string FirstName { get; set; }

        [Required, MaxLength(50)]
        public string LastName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
        [Required]
        public string PasswordSalt { get; set; } // Store the salt used for hashing
        [Required]
        public string Role { get; set; } // Admin, User

        public List<BorrowedBook> BorrowedBooks { get; set; }
        public List<PurchasedBook> PurchasedBooks { get; set; }
        public int MaxBorrowableBooks { get; set; } = 3; // Limit to 3 books
    }
}
