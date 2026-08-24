using FlyBook.Data;
using FlyBook.Models;
using FlyBook.Services;
using FlyBook.ViewModels;
using FlyBook.ViewModels.Admin;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FlyBook.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var action = context.RouteData.Values["action"]?.ToString();
            var isPublicAction =
                string.Equals(action, nameof(Login), StringComparison.OrdinalIgnoreCase) ||
                string.Equals(action, nameof(Logout), StringComparison.OrdinalIgnoreCase);

            if (!isPublicAction && !IsAdmin())
            {
                context.Result = RedirectToAction(nameof(Login));
                return;
            }

            if (!isPublicAction)
            {
                SetNoCacheHeaders();
            }

            base.OnActionExecuting(context);
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (IsAdmin())
            {
                return RedirectToAction(nameof(Index));
            }

            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim().ToLower();
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null || user.Role != "Admin")
            {
                ModelState.AddModelError(string.Empty, "Invalid admin email or password.");
                return View(model);
            }

            var result = PasswordHelper.VerifyPassword(user, model.Password);

            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "Invalid admin email or password.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "This admin account is currently disabled.");
                return View(model);
            }

            HttpContext.Session.Clear();

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.Password = PasswordHelper.HashPassword(model.Password);
                await _context.SaveChangesAsync();
            }

            SignInAdmin(user);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            SetNoCacheHeaders();

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalFlights = await _context.Flights.CountAsync();
            ViewBag.TotalBookings = await _context.Bookings.CountAsync();
            ViewBag.TotalRevenue = await _context.Payments
                .Where(p => p.PaymentStatus == "Paid")
                .SumAsync(p => (decimal?)p.Amount) ?? 0;
            ViewBag.PendingBookings = await _context.Bookings.CountAsync(b => b.Status == "Pending");
            ViewBag.ConfirmedBookings = await _context.Bookings.CountAsync(b => b.Status == "Confirmed");
            ViewBag.CancelledBookings = await _context.Bookings.CountAsync(b => b.Status == "Cancelled");
            ViewBag.PaidPayments = await _context.Payments.CountAsync(p => p.PaymentStatus == "Paid");

            var recentBookings = await GetBookingQuery()
                .AsNoTracking()
                .OrderByDescending(b => b.BookingDate)
                .Take(10)
                .ToListAsync();

            return View(recentBookings);
        }

        [HttpGet]
        public async Task<IActionResult> Flights()
        {
            var flights = await _context.Flights
                .AsNoTracking()
                .Include(f => f.Airline)
                .Include(f => f.FromAirport)
                .Include(f => f.ToAirport)
                .OrderBy(f => f.DepartureDateTime)
                .ToListAsync();

            return View(flights);
        }

        [HttpGet]
        public async Task<IActionResult> CreateFlight()
        {
            await LoadFlightLists();

            return View(new AdminFlightViewModel
            {
                DepartureDateTime = DateTime.Now.AddDays(1),
                ArrivalDateTime = DateTime.Now.AddDays(1).AddHours(2)
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateFlight(AdminFlightViewModel model)
        {
            await ValidateFlightModel(model);

            if (!ModelState.IsValid)
            {
                await LoadFlightLists(model.AirlineId, model.FromAirportId, model.ToAirportId);
                return View(model);
            }

            _context.Flights.Add(new Flight
            {
                FlightNumber = model.FlightNumber.Trim(),
                AirlineId = model.AirlineId,
                FromAirportId = model.FromAirportId,
                ToAirportId = model.ToAirportId,
                DepartureDateTime = model.DepartureDateTime,
                ArrivalDateTime = model.ArrivalDateTime,
                Price = model.Price,
                AvailableSeats = model.AvailableSeats,
                AircraftType = model.AircraftType?.Trim()
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Flight was created successfully.";
            return RedirectToAction(nameof(Flights));
        }

        [HttpGet]
        public async Task<IActionResult> EditFlight(int id)
        {
            var flight = await _context.Flights
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.FlightId == id);

            if (flight == null)
            {
                return NotFound();
            }

            await LoadFlightLists(flight.AirlineId, flight.FromAirportId, flight.ToAirportId);

            return View(new AdminFlightViewModel
            {
                FlightNumber = flight.FlightNumber,
                AirlineId = flight.AirlineId,
                FromAirportId = flight.FromAirportId,
                ToAirportId = flight.ToAirportId,
                DepartureDateTime = flight.DepartureDateTime,
                ArrivalDateTime = flight.ArrivalDateTime,
                Price = flight.Price,
                AvailableSeats = flight.AvailableSeats,
                AircraftType = flight.AircraftType
            });
        }

        [HttpPost]
        public async Task<IActionResult> EditFlight(int id, AdminFlightViewModel model)
        {
            await ValidateFlightModel(model, id);

            if (!ModelState.IsValid)
            {
                await LoadFlightLists(model.AirlineId, model.FromAirportId, model.ToAirportId);
                return View(model);
            }

            var flight = await _context.Flights.FirstOrDefaultAsync(f => f.FlightId == id);

            if (flight == null)
            {
                return NotFound();
            }

            flight.FlightNumber = model.FlightNumber.Trim();
            flight.AirlineId = model.AirlineId;
            flight.FromAirportId = model.FromAirportId;
            flight.ToAirportId = model.ToAirportId;
            flight.DepartureDateTime = model.DepartureDateTime;
            flight.ArrivalDateTime = model.ArrivalDateTime;
            flight.Price = model.Price;
            flight.AvailableSeats = model.AvailableSeats;
            flight.AircraftType = model.AircraftType?.Trim();

            await _context.SaveChangesAsync();

            TempData["Success"] = "Flight was updated successfully.";
            return RedirectToAction(nameof(Flights));
        }

        [HttpGet]
        public async Task<IActionResult> DeleteFlight(int id)
        {
            var flight = await _context.Flights
                .AsNoTracking()
                .Include(f => f.Airline)
                .Include(f => f.FromAirport)
                .Include(f => f.ToAirport)
                .FirstOrDefaultAsync(f => f.FlightId == id);

            if (flight == null)
            {
                return NotFound();
            }

            return View(flight);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFlightConfirmed(int id)
        {
            var flight = await _context.Flights
                .Include(f => f.Bookings)
                .FirstOrDefaultAsync(f => f.FlightId == id);

            if (flight == null)
            {
                return NotFound();
            }

            if (flight.Bookings.Any())
            {
                TempData["Error"] = "This flight has bookings and cannot be deleted.";
                return RedirectToAction(nameof(Flights));
            }

            _context.Flights.Remove(flight);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Flight was deleted successfully.";
            return RedirectToAction(nameof(Flights));
        }

        [HttpGet]
        public async Task<IActionResult> Airlines()
        {
            var airlines = await _context.Airlines
                .AsNoTracking()
                .OrderBy(a => a.Name)
                .ToListAsync();

            return View(airlines);
        }

        [HttpGet]
        public IActionResult CreateAirline()
        {
            return View(new AdminAirlineViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> CreateAirline(AdminAirlineViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var name = model.Name.Trim();
            var exists = await _context.Airlines
                .AnyAsync(a => a.Name.ToLower() == name.ToLower());

            if (exists)
            {
                ModelState.AddModelError(nameof(model.Name), "Airline already exists.");
                return View(model);
            }

            _context.Airlines.Add(new Airline
            {
                Name = name,
                Logo = model.Logo?.Trim()
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Airline was created successfully.";
            return RedirectToAction(nameof(Airlines));
        }

        [HttpGet]
        public async Task<IActionResult> EditAirline(int id)
        {
            var airline = await _context.Airlines
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AirlineId == id);

            if (airline == null)
            {
                return NotFound();
            }

            return View(new AdminAirlineViewModel
            {
                Name = airline.Name,
                Logo = airline.Logo
            });
        }

        [HttpPost]
        public async Task<IActionResult> EditAirline(int id, AdminAirlineViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var airline = await _context.Airlines.FirstOrDefaultAsync(a => a.AirlineId == id);

            if (airline == null)
            {
                return NotFound();
            }

            var name = model.Name.Trim();
            var exists = await _context.Airlines
                .AnyAsync(a => a.AirlineId != id && a.Name.ToLower() == name.ToLower());

            if (exists)
            {
                ModelState.AddModelError(nameof(model.Name), "Airline already exists.");
                return View(model);
            }

            airline.Name = name;
            airline.Logo = model.Logo?.Trim();

            await _context.SaveChangesAsync();

            TempData["Success"] = "Airline was updated successfully.";
            return RedirectToAction(nameof(Airlines));
        }

        [HttpGet]
        public async Task<IActionResult> DeleteAirline(int id)
        {
            var airline = await _context.Airlines
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AirlineId == id);

            if (airline == null)
            {
                return NotFound();
            }

            return View(airline);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAirlineConfirmed(int id)
        {
            var airline = await _context.Airlines.FirstOrDefaultAsync(a => a.AirlineId == id);

            if (airline == null)
            {
                return NotFound();
            }

            var inUse = await _context.Flights.AnyAsync(f => f.AirlineId == id);

            if (inUse)
            {
                TempData["Error"] = "This airline is used by flights and cannot be deleted.";
                return RedirectToAction(nameof(Airlines));
            }

            _context.Airlines.Remove(airline);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Airline was deleted successfully.";
            return RedirectToAction(nameof(Airlines));
        }

        [HttpGet]
        public async Task<IActionResult> Airports()
        {
            var airports = await _context.Airports
                .AsNoTracking()
                .OrderBy(a => a.City)
                .ToListAsync();

            return View(airports);
        }

        [HttpGet]
        public IActionResult CreateAirport()
        {
            return View(new AdminAirportViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> CreateAirport(AdminAirportViewModel model)
        {
            ValidateAirportModel(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var code = model.Code.Trim().ToUpper();
            var exists = await _context.Airports
                .AnyAsync(a => a.Code.ToUpper() == code);

            if (exists)
            {
                ModelState.AddModelError(nameof(model.Code), "Airport code already exists.");
                return View(model);
            }

            _context.Airports.Add(new Airport
            {
                Name = model.Name.Trim(),
                Code = code,
                City = model.City.Trim(),
                Country = model.Country.Trim()
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Airport was created successfully.";
            return RedirectToAction(nameof(Airports));
        }

        [HttpGet]
        public async Task<IActionResult> EditAirport(int id)
        {
            var airport = await _context.Airports
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AirportId == id);

            if (airport == null)
            {
                return NotFound();
            }

            return View(new AdminAirportViewModel
            {
                Name = airport.Name,
                Code = airport.Code,
                City = airport.City,
                Country = airport.Country
            });
        }

        [HttpPost]
        public async Task<IActionResult> EditAirport(int id, AdminAirportViewModel model)
        {
            ValidateAirportModel(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var airport = await _context.Airports.FirstOrDefaultAsync(a => a.AirportId == id);

            if (airport == null)
            {
                return NotFound();
            }

            var code = model.Code.Trim().ToUpper();
            var exists = await _context.Airports
                .AnyAsync(a => a.AirportId != id && a.Code.ToUpper() == code);

            if (exists)
            {
                ModelState.AddModelError(nameof(model.Code), "Airport code already exists.");
                return View(model);
            }

            airport.Name = model.Name.Trim();
            airport.Code = code;
            airport.City = model.City.Trim();
            airport.Country = model.Country.Trim();

            await _context.SaveChangesAsync();

            TempData["Success"] = "Airport was updated successfully.";
            return RedirectToAction(nameof(Airports));
        }

        [HttpGet]
        public async Task<IActionResult> DeleteAirport(int id)
        {
            var airport = await _context.Airports
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AirportId == id);

            if (airport == null)
            {
                return NotFound();
            }

            return View(airport);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAirportConfirmed(int id)
        {
            var airport = await _context.Airports.FirstOrDefaultAsync(a => a.AirportId == id);

            if (airport == null)
            {
                return NotFound();
            }

            var inUse = await _context.Flights
                .AnyAsync(f => f.FromAirportId == id || f.ToAirportId == id);

            if (inUse)
            {
                TempData["Error"] = "This airport is used by flights and cannot be deleted.";
                return RedirectToAction(nameof(Airports));
            }

            _context.Airports.Remove(airport);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Airport was deleted successfully.";
            return RedirectToAction(nameof(Airports));
        }

        [HttpGet]
        public async Task<IActionResult> Bookings()
        {
            var bookings = await GetBookingQuery()
                .AsNoTracking()
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(bookings);
        }

        [HttpGet]
        public async Task<IActionResult> Payments()
        {
            var payments = await _context.Payments
                .AsNoTracking()
                .Include(p => p.Booking!)
                    .ThenInclude(b => b.User)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return View(payments);
        }

        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                .AsNoTracking()
                .Include(u => u.Bookings)
                .OrderBy(u => u.Role)
                .ThenBy(u => u.FullName)
                .ToListAsync();

            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableUser(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                TempData["Error"] = "User was not found.";
                return RedirectToAction(nameof(Users));
            }

            if (IsProtectedUser(user))
            {
                TempData["Error"] = "Admin accounts cannot be disabled.";
                return RedirectToAction(nameof(Users));
            }

            if (IsCurrentAdmin(user.UserId))
            {
                TempData["Error"] = "You cannot disable your own account.";
                return RedirectToAction(nameof(Users));
            }

            user.IsActive = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = "User account disabled successfully.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableUser(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                TempData["Error"] = "User was not found.";
                return RedirectToAction(nameof(Users));
            }

            if (IsProtectedUser(user))
            {
                TempData["Error"] = "Admin accounts are protected.";
                return RedirectToAction(nameof(Users));
            }

            if (IsCurrentAdmin(user.UserId))
            {
                TempData["Error"] = "You cannot change your own account status here.";
                return RedirectToAction(nameof(Users));
            }

            user.IsActive = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "User account enabled successfully.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                TempData["Error"] = "User was not found.";
                return RedirectToAction(nameof(Users));
            }

            if (IsProtectedUser(user))
            {
                TempData["Error"] = "Admin accounts cannot be deleted.";
                return RedirectToAction(nameof(Users));
            }

            if (IsCurrentAdmin(user.UserId))
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Users));
            }

            var hasBookings = await _context.Bookings
                .AnyAsync(b => b.UserId == user.UserId);

            if (hasBookings)
            {
                TempData["Error"] = "This user has related records and cannot be deleted. Disable the account instead.";
                return RedirectToAction(nameof(Users));
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "User deleted successfully.";
            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public async Task<IActionResult> Ticket(int bookingId)
        {
            var booking = await GetBookingQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.Ticket == null)
            {
                TempData["Error"] = "This booking does not have a ticket yet.";
                return RedirectToAction(nameof(Bookings));
            }

            return View("~/Views/Booking/Ticket.cshtml", booking);
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("Role") == "Admin";
        }

        private bool IsProtectedUser(User user)
        {
            return user.Role == "Admin";
        }

        private bool IsCurrentAdmin(int userId)
        {
            return HttpContext.Session.GetInt32("UserId") == userId;
        }

        private void SignInAdmin(User user)
        {
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserName", user.FullName);
            HttpContext.Session.SetString("Role", "Admin");
            HttpContext.Session.SetString("UserRole", "Admin");
        }

        private void SetNoCacheHeaders()
        {
            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";
        }

        private IQueryable<Booking> GetBookingQuery()
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
                    .ThenInclude(f => f.ToAirport);
        }

        private async Task LoadFlightLists(int? airlineId = null, int? fromAirportId = null, int? toAirportId = null)
        {
            ViewBag.Airlines = new SelectList(
                await _context.Airlines
                    .AsNoTracking()
                    .OrderBy(a => a.Name)
                    .ToListAsync(),
                "AirlineId",
                "Name",
                airlineId);

            ViewBag.Airports = new SelectList(
                await _context.Airports
                    .AsNoTracking()
                    .OrderBy(a => a.City)
                    .Select(a => new
                    {
                        a.AirportId,
                        Name = a.City + " (" + a.Code + ")"
                    })
                    .ToListAsync(),
                "AirportId",
                "Name",
                fromAirportId);

            ViewBag.ToAirports = new SelectList(
                await _context.Airports
                    .AsNoTracking()
                    .OrderBy(a => a.City)
                    .Select(a => new
                    {
                        a.AirportId,
                        Name = a.City + " (" + a.Code + ")"
                    })
                    .ToListAsync(),
                "AirportId",
                "Name",
                toAirportId);
        }

        private async Task ValidateFlightModel(AdminFlightViewModel model, int? flightId = null)
        {
            if (model.FromAirportId == model.ToAirportId)
            {
                ModelState.AddModelError(nameof(model.ToAirportId), "From Airport and To Airport must be different.");
            }

            if (model.ArrivalDateTime <= model.DepartureDateTime)
            {
                ModelState.AddModelError(nameof(model.ArrivalDateTime), "Arrival time must be after departure time.");
            }

            var airlineExists = await _context.Airlines.AnyAsync(a => a.AirlineId == model.AirlineId);
            var fromAirportExists = await _context.Airports.AnyAsync(a => a.AirportId == model.FromAirportId);
            var toAirportExists = await _context.Airports.AnyAsync(a => a.AirportId == model.ToAirportId);

            if (!airlineExists)
            {
                ModelState.AddModelError(nameof(model.AirlineId), "Please select a valid airline.");
            }

            if (!fromAirportExists)
            {
                ModelState.AddModelError(nameof(model.FromAirportId), "Please select a valid departure airport.");
            }

            if (!toAirportExists)
            {
                ModelState.AddModelError(nameof(model.ToAirportId), "Please select a valid arrival airport.");
            }

            var flightNumber = model.FlightNumber?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(flightNumber))
            {
                var duplicateQuery = _context.Flights
                    .Where(f => f.FlightNumber.ToLower() == flightNumber.ToLower());

                if (flightId.HasValue)
                {
                    duplicateQuery = duplicateQuery
                        .Where(f => f.FlightId != flightId.Value);
                }

                var duplicateFlightNumber = await duplicateQuery.AnyAsync();

                if (duplicateFlightNumber)
                {
                    ModelState.AddModelError(nameof(model.FlightNumber), "Flight number already exists.");
                }
            }
        }

        private void ValidateAirportModel(AdminAirportViewModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.Code) && model.Code.Trim().Length < 3)
            {
                ModelState.AddModelError(nameof(model.Code), "Airport code must be at least 3 characters.");
            }
        }
    }
}
