using System.ComponentModel.DataAnnotations;

namespace eLibraryApp.Models
{
    public class WaitingListEntry
    {
        [Key]
        public int WaitingListEntryId { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }

        [Required]
        public int BookId { get; set; }
        public Book Book { get; set; }

        [Required]
        public DateTime RequestedDate { get; set; }
        public bool NotificationSent { get; set; } // Tracks if the user was notified about book availability
    }
}
