using System.ComponentModel.DataAnnotations;

namespace FlyBook.ViewModels.Admin
{
    public class AdminFlightViewModel
    {
        [Required]
        [StringLength(20)]
        public string FlightNumber { get; set; } = string.Empty;

        [Required]
        public int AirlineId { get; set; }

        [Required]
        public int FromAirportId { get; set; }

        [Required]
        public int ToAirportId { get; set; }

        [Required]
        public DateTime DepartureDateTime { get; set; }

        [Required]
        public DateTime ArrivalDateTime { get; set; }

        [Required]
        [Range(0.01, 100000)]
        public decimal Price { get; set; }

        [Required]
        [Range(0, 1000)]
        public int AvailableSeats { get; set; }

        [StringLength(100)]
        public string? AircraftType { get; set; }
    }
}
