namespace MovieTicketAppFinal.Models
{
    public class ShowOption
    {
        public int ShowId { get; set; }
        public string DisplayText { get; set; } = "";
        public int MovieId { get; set; }
        public int HallId { get; set; }
        public int TheatreId { get; set; }
    }

    public class UserTicketResult
    {
        public string CustomerName { get; set; } = "";
        public string CustomerAddress { get; set; } = "";
        public string ContactNumber { get; set; } = "";
        public int TicketId { get; set; }
        public string TicketStatus { get; set; } = "";
        public string SeatNo { get; set; } = "";
        public DateTime? BookingTime { get; set; }
        public DateTime? PurchaseTime { get; set; }
        public string MovieName { get; set; } = "";
        public string TheaterName { get; set; } = "";
        public string HallName { get; set; } = "";
        public DateTime? ShowTiming { get; set; }
        public decimal? TicketPrice { get; set; }
        public string? PolicyName { get; set; }
    }

    public class TheaterMovieResult
    {
        public string TheaterName { get; set; } = "";
        public string HallName { get; set; } = "";
        public int HallCapacity { get; set; }
        public string MovieName { get; set; } = "";
        public string Language { get; set; } = "";
        public string Genre { get; set; } = "";
        public DateTime? ReleaseDate { get; set; }
        public int Duration { get; set; }
        public DateTime? ShowTiming { get; set; }
        public int ShowDuration { get; set; }
    }

    public class OccupancyResult
    {
        public int Rank { get; set; }
        public string TheaterName { get; set; } = "";
        public string HallName { get; set; } = "";
        public int HallCapacity { get; set; }
        public int PaidTickets { get; set; }
        public double OccupancyPct { get; set; }
    }
}