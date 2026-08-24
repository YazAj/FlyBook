using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlyBook.Models
{
    public class Booking
    {
        public int BookingId { get; set; }

        [Required]
        [StringLength(30)]
        public string BookingNumber { get; set; } = string.Empty;

        [Required]
        public int UserId { get; set; }

        public User? User { get; set; }

        [Required]
        public int FlightId { get; set; }

        public Flight? Flight { get; set; }

        [Required]
        public int PassengerId { get; set; }

        public Passenger? Passenger { get; set; }

        [Required]
        public DateTime BookingDate { get; set; } = DateTime.Now;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        public Payment? Payment { get; set; }

        public Ticket? Ticket { get; set; }
    }
}