namespace MovieTicketAppFinal.Models
{
    public class Theater
    {
        public int TheatreId { get; set; }
        public string? TheatreName { get; set; }
        public string? TheatreAddress { get; set; }
    }

    public class Hall
    {
        public int HallId { get; set; }
        public int TheatreId { get; set; }
        public string? HallName { get; set; }
        public int? HallCapacity { get; set; }
    }

    public class TicketPolicy
    {
        public int TicketPolicyId { get; set; }
        public decimal TicketBasePrice { get; set; }
        public decimal? TicketPolicyPrice { get; set; }
        public string? TicketPaymentPolicy { get; set; }
    }

    public class Cancellation
    {
        public int CancellationId { get; set; }
        public int? TicketId { get; set; }
        public DateTime? CancellationTime { get; set; }
        public string? CancellationReason { get; set; }
    }

    public class Show
    {
        public int ShowId { get; set; }
        public int MovieId { get; set; }
        public int HallId { get; set; }
        public int? TicketPolicyId { get; set; }
        public DateTime? ShowTiming { get; set; }
        public int? ShowDuration { get; set; }
    }
}