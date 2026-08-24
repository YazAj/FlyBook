using FlyBook.Data;
using FlyBook.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlyBook.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var auth = await EnsureNormalUserAsync();

            if (auth.Redirect != null)
            {
                return auth.Redirect;
            }

            var userId = auth.UserId;
            var bookings = await _context.Bookings
                .AsNoTracking()
                .Include(b => b.Flight!)
                    .ThenInclude(f => f.Airline)
                .Include(b => b.Flight!)
                    .ThenInclude(f => f.FromAirport)
                .Include(b => b.Flight!)
                    .ThenInclude(f => f.ToAirport)
                .Include(b => b.Passenger)
                .Include(b => b.Payment)
                .Include(b => b.Ticket)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(bookings);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var auth = await EnsureNormalUserAsync();

            if (auth.Redirect != null)
            {
                return auth.Redirect;
            }

            var userId = auth.UserId;
            var booking = await GetOwnedBookingQuery(userId)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                TempData["Error"] = "Booking was not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(booking);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int flightId)
        {
            var auth = await EnsureNormalUserAsync();

            if (auth.Redirect != null)
            {
                return auth.Redirect;
            }

            var flight = await GetFlightWithDetails()
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.FlightId == flightId);

            if (flight == null)
            {
                return NotFound();
            }

            if (flight.AvailableSeats <= 0)
            {
                TempData["Error"] = "No seats are available for this flight.";
                return RedirectToAction("Index", "Flights");
            }

            ViewBag.Flight = flight;

            return View(new Passenger());
        }

        [HttpPost]
        public async Task<IActionResult> Create(int flightId, Passenger passenger)
        {
            var auth = await EnsureNormalUserAsync();

            if (auth.Redirect != null)
            {
                return auth.Redirect;
            }

            var userId = auth.UserId;
            var flight = await GetFlightWithDetails()
                .FirstOrDefaultAsync(f => f.FlightId == flightId);

            if (flight == null)
            {
                return NotFound();
            }

            ValidatePassenger(passenger);

            if (flight.AvailableSeats <= 0)
            {
                ModelState.AddModelError(string.Empty, "No seats are available for this flight.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Flight = flight;
                return View(passenger);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            _context.Passengers.Add(passenger);
            await _context.SaveChangesAsync();

            flight.AvailableSeats -= 1;

            var booking = new Booking
            {
                BookingNumber = "FB-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
                FlightId = flight.FlightId,
                PassengerId = passenger.PassengerId,
                UserId = userId,
                BookingDate = DateTime.Now,
                TotalPrice = flight.Price,
                Status = "Pending"
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return RedirectToAction("Pay", "Payment", new { bookingId = booking.BookingId });
        }

        [HttpGet]
        public IActionResult EditBooking(int id)
        {
            return BlockUserBookingChanges();
        }

        [HttpPost]
        public IActionResult EditBooking(Booking booking)
        {
            return BlockUserBookingChanges();
        }

        [HttpGet]
        public IActionResult CancelBooking(int id)
        {
            return BlockUserBookingChanges();
        }

        [HttpPost]
        public IActionResult CancelBookingConfirmed(int id)
        {
            return BlockUserBookingChanges();
        }

        [HttpGet]
        public async Task<IActionResult> Ticket(int id)
        {
            var auth = await EnsureNormalUserAsync();

            if (auth.Redirect != null)
            {
                return auth.Redirect;
            }

            var userId = auth.UserId;
            var booking = await GetOwnedBookingQuery(userId)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                TempData["Error"] = "Booking was not found.";
                return RedirectToAction(nameof(Index));
            }

            if (booking.Payment?.PaymentStatus != "Paid" || booking.Ticket == null)
            {
                TempData["Error"] = "Ticket is available after successful payment.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(booking);
        }

        private IActionResult BlockUserBookingChanges()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (HttpContext.Session.GetString("Role") == "Admin")
            {
                return RedirectToAction("Bookings", "Admin");
            }

            TempData["Error"] = "Booking edit and cancellation are not available.";
            return RedirectToAction(nameof(Index));
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

        private IQueryable<Flight> GetFlightWithDetails()
        {
            return _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.FromAirport)
                .Include(f => f.ToAirport);
        }

        private IQueryable<Booking> GetOwnedBookingQuery(int userId)
        {
            return _context.Bookings
                .Include(b => b.User)
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

        private void ValidatePassenger(Passenger passenger)
        {
            if (passenger.DateOfBirth >= DateTime.Today)
            {
                ModelState.AddModelError(nameof(passenger.DateOfBirth), "Date of birth must be in the past.");
            }
        }
    }
}
