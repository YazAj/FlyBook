using System.ComponentModel.DataAnnotations;

namespace FlyBook.ViewModels
{
    public class PaymentViewModel
    {
        [Required]
        public int BookingId { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Card";

        [Required]
        [StringLength(25, MinimumLength = 12)]
        public string CardNumber { get; set; } = "";

        [Required]
        [StringLength(5, MinimumLength = 5)]
        public string ExpiryDate { get; set; } = "";

        [Required]
        [StringLength(4, MinimumLength = 3)]
        public string CVV { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string CardHolderName { get; set; } = "";
    }
}
