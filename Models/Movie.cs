namespace MovieTicketAppFinal.Models
{
    public class Movie
    {
        public int MovieId { get; set; }
        public string MovieName { get; set; }
        public string MovieLanguage { get; set; }
        public DateTime? MovieReleaseDate { get; set; }
        public int? MovieDuration { get; set; }
        public string MovieGenre { get; set; }
    }
}