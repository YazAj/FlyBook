using System.ComponentModel.DataAnnotations;

namespace FlyBook.ViewModels.Admin
{
    public class AdminAirlineViewModel
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Logo { get; set; }
    }
}