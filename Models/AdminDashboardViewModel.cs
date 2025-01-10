namespace eLibraryApp.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalBooks { get; set; }
        public int BorrowedBooks { get; set; }
        public List<User> RecentUsers { get; set; }
    }
}
