using System.ComponentModel.DataAnnotations;

namespace FlyBook.Models
{
    public class Ticket
    {
        public int TicketId { get; set; }

        [Required]
        public int BookingId { get; set; }

        public Booking? Booking { get; set; }

        [Required]
        [StringLength(50)]
        public string TicketNumber { get; set; } = string.Empty;

        [Required]
        public DateTime IssueDate { get; set; } = DateTime.Now;
    }
}