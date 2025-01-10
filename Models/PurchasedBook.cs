using System.ComponentModel.DataAnnotations;

namespace eLibraryApp.Models
{
    public class PurchasedBook
    {
        [Key]
        public int PurchasedBookId { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        public int BookId { get; set; }
        public Book Book { get; set; }

        [Required]
        public DateTime PurchaseDate { get; set; }
    }
}
