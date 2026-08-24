using System.ComponentModel.DataAnnotations;

namespace FlyBook.Models
{
    public class Airline
    {
        public int AirlineId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Logo { get; set; }

        public ICollection<Flight> Flights { get; set; } = new List<Flight>();
    }
}