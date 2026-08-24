using System.ComponentModel.DataAnnotations;

namespace FlyBook.Models
{
    public class Airport
    {
        public int AirportId { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Country { get; set; } = string.Empty;

        public ICollection<Flight> DepartureFlights { get; set; } = new List<Flight>();

        public ICollection<Flight> ArrivalFlights { get; set; } = new List<Flight>();
    }
}