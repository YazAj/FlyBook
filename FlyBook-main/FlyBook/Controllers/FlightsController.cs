using FlyBook.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlyBook.Controllers
{
    public class FlightsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FlightsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var flights = await GetFlightQuery()
                .AsNoTracking()
                .OrderBy(f => f.DepartureDateTime)
                .ToListAsync();

            return View(flights);
        }

        [HttpGet]
        public async Task<IActionResult> Search(int? fromAirportId, int? toAirportId, DateTime? departureDate)
        {
            if (fromAirportId == null || toAirportId == null || departureDate == null)
            {
                TempData["Error"] = "Please select departure, destination, and date.";
                return RedirectToAction("Index", "Home");
            }

            if (fromAirportId == toAirportId)
            {
                TempData["Error"] = "Departure and destination airports must be different.";
                return RedirectToAction("Index", "Home");
            }

            var flights = await GetFlightQuery()
                .AsNoTracking()
                .Where(f =>
                    f.FromAirportId == fromAirportId.Value &&
                    f.ToAirportId == toAirportId.Value &&
                    f.DepartureDateTime.Date == departureDate.Value.Date)
                .OrderBy(f => f.DepartureDateTime)
                .ToListAsync();

            return View("Index", flights);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var flight = await GetFlightQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.FlightId == id);

            if (flight == null)
            {
                return NotFound();
            }

            return View(flight);
        }

        private IQueryable<Models.Flight> GetFlightQuery()
        {
            return _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.FromAirport)
                .Include(f => f.ToAirport);
        }
    }
}
