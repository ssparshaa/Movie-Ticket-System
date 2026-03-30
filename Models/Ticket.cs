namespace MovieTicketAppFinal.Models
{
    public class Ticket
    {
        public int TicketId { get; set; }
        public string? TicketStatus { get; set; }
        public DateTime? TicketBookingTime { get; set; }
        public DateTime? TicketPurchaseTime { get; set; }
        public string? SeatNo { get; set; }
    }
}