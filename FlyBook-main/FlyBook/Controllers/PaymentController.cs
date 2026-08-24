using FlyBook.Data;
using FlyBook.Models;
using FlyBook.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlyBook.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Pay(int bookingId)
        {
            var auth = await EnsureNormalUserAsync();

            if (auth.Redirect != null)
            {
                return auth.Redirect;
            }

            var userId = auth.UserId;
            var booking = await GetOwnedBookingQuery(userId)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                TempData["Error"] = "Booking was not found.";
                return RedirectToAction("Index", "Booking");
            }

            if (booking.Payment?.PaymentStatus == "Paid" && booking.Ticket != null)
            {
                return RedirectToAction("Ticket", "Booking", new { id = booking.BookingId });
            }

            if (booking.Status == "Cancelled")
            {
                TempData["Error"] = "Cancelled bookings cannot be paid.";
                return RedirectToAction("Details", "Booking", new { id = booking.BookingId });
            }

            return View(booking);
        }

        [HttpPost]
        public async Task<IActionResult> PayConfirmed(PaymentViewModel model)
        {
            var auth = await EnsureNormalUserAsync();

            if (auth.Redirect != null)
            {
                return auth.Redirect;
            }

            var userId = auth.UserId;
            var booking = await GetOwnedBookingQuery(userId)
                .FirstOrDefaultAsync(b => b.BookingId == model.BookingId);

            if (booking == null)
            {
                TempData["Error"] = "Booking was not found.";
                return RedirectToAction("Index", "Booking");
            }

            if (booking.Payment?.PaymentStatus == "Paid" && booking.Ticket != null)
            {
                return RedirectToAction("Ticket", "Booking", new { id = booking.BookingId });
            }

            if (booking.Status == "Cancelled")
            {
                TempData["Error"] = "Cancelled bookings cannot be paid.";
                return RedirectToAction("Details", "Booking", new { id = booking.BookingId });
            }

            if (model.PaymentMethod != "Card")
            {
                ModelState.AddModelError(nameof(model.PaymentMethod), "Invalid payment method.");
            }

            if (!ModelState.IsValid)
            {
                return View("Pay", booking);
            }

            if (booking.Payment == null)
            {
                _context.Payments.Add(new Payment
                {
                    BookingId = booking.BookingId,
                    Amount = booking.TotalPrice,
                    PaymentDate = DateTime.Now,
                    PaymentStatus = "Paid",
                    PaymentMethod = "Card"
                });
            }
            else
            {
                booking.Payment.Amount = booking.TotalPrice;
                booking.Payment.PaymentDate = DateTime.Now;
                booking.Payment.PaymentStatus = "Paid";
                booking.Payment.PaymentMethod = "Card";
            }

            booking.Status = "Confirmed";

            if (booking.Ticket == null)
            {
                _context.Tickets.Add(new Ticket
                {
                    BookingId = booking.BookingId,
                    TicketNumber = "TKT-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
                    IssueDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Ticket", "Booking", new { id = booking.BookingId });
        }

        private async Task<(IActionResult? Redirect, int UserId)> EnsureNormalUserAsync()
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");

            if (sessionUserId == null)
            {
                return (RedirectToAction("Login", "Account"), 0);
            }

            if (HttpContext.Session.GetString("Role") != "User")
            {
                return (RedirectToAction("Index", "Admin"), 0);
            }

            var isActiveUser = await _context.Users
                .AnyAsync(u => u.UserId == sessionUserId.Value && u.Role == "User" && u.IsActive);

            if (!isActiveUser)
            {
                HttpContext.Session.Clear();
                TempData["LoginError"] = GetDisabledAccountMessage();

                return (RedirectToAction("Login", "Account"), 0);
            }

            return (null, sessionUserId.Value);
        }

        private static string GetDisabledAccountMessage()
        {
            return "Unable to sign in. Your account is currently disabled. Please contact the administrator.";
        }

        private IQueryable<Booking> GetOwnedBookingQuery(int userId)
        {
            return _context.Bookings
                .Include(b => b.Passenger)
                .Include(b => b.Payment)
                .Include(b => b.Ticket)
                .Include(b => b.Flight!)
                    .ThenInclude(f => f.Airline)
                .Include(b => b.Flight!)
                    .ThenInclude(f => f.FromAirport)
                .Include(b => b.Flight!)
                    .ThenInclude(f => f.ToAirport)
                .Where(b => b.UserId == userId);
        }
    }
}
