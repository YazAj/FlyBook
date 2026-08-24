using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlyBook.Models
{
    public class Flight
    {
        public int FlightId { get; set; }

        [Required]
        [StringLength(20)]
        public string FlightNumber { get; set; } = string.Empty;

        [Required]
        public int AirlineId { get; set; }

        public Airline? Airline { get; set; }

        [Required]
        public int FromAirportId { get; set; }

        [Required]
        public int ToAirportId { get; set; }

        public Airport? FromAirport { get; set; }

        public Airport? ToAirport { get; set; }

        [Required]
        public DateTime DepartureDateTime { get; set; }

        [Required]
        public DateTime ArrivalDateTime { get; set; }

        [Required]
        [Range(0.01, 100000)]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Required]
        [Range(0, 1000)]
        public int AvailableSeats { get; set; }

        [StringLength(100)]
        public string? AircraftType { get; set; }

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
