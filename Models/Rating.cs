using System.ComponentModel.DataAnnotations;

namespace eLibraryApp.Models
{
    public class Rating
    {
        [Key]
        public int RatingId { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        public int BookId { get; set; }
        public Book Book { get; set; }

        [Required, Range(1, 5)]
        public int RatingValue { get; set; }

        [MaxLength(500)]
        public string Feedback { get; set; }
        [Required]
        public DateTime RatingDate { get; set; } = DateTime.Now; 
    }
}
