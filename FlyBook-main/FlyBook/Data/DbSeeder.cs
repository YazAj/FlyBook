using FlyBook.Models;
using FlyBook.Services;
using Microsoft.EntityFrameworkCore;

namespace FlyBook.Data
{
    public static class DbSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            context.Database.Migrate();

            EnsureUserSecurity(context);

            if (!context.Airlines.Any())
            {
                context.Airlines.AddRange(
                    new Airline { Name = "FlyDubai", Logo = "/images/airlines/flydubai.png" },
                    new Airline { Name = "Emirates", Logo = "/images/airlines/emirates.png" },
                    new Airline { Name = "Royal Jordanian", Logo = "/images/airlines/royaljordanian.png" },
                    new Airline { Name = "Qatar Airways", Logo = "/images/airlines/qatarairways.png" }
                );

                context.SaveChanges();
            }

            if (!context.Airports.Any())
            {
                context.Airports.AddRange(
                    new Airport
                    {
                        Name = "Queen Alia International Airport",
                        Code = "AMM",
                        City = "Amman",
                        Country = "Jordan"
                    },
                    new Airport
                    {
                        Name = "Dubai International Airport",
                        Code = "DXB",
                        City = "Dubai",
                        Country = "United Arab Emirates"
                    },
                    new Airport
                    {
                        Name = "Istanbul Airport",
                        Code = "IST",
                        City = "Istanbul",
                        Country = "Turkey"
                    },
                    new Airport
                    {
                        Name = "Hamad International Airport",
                        Code = "DOH",
                        City = "Doha",
                        Country = "Qatar"
                    }
                );

                context.SaveChanges();
            }

            if (!context.Flights.Any())
            {
                var flyDubai = context.Airlines.First(a => a.Name == "FlyDubai");
                var emirates = context.Airlines.First(a => a.Name == "Emirates");
                var royalJordanian = context.Airlines.First(a => a.Name == "Royal Jordanian");
                var qatarAirways = context.Airlines.First(a => a.Name == "Qatar Airways");

                var amm = context.Airports.First(a => a.Code == "AMM");
                var dxb = context.Airports.First(a => a.Code == "DXB");
                var ist = context.Airports.First(a => a.Code == "IST");
                var doh = context.Airports.First(a => a.Code == "DOH");

                context.Flights.AddRange(
                    new Flight
                    {
                        FlightNumber = "FZ 121",
                        AirlineId = flyDubai.AirlineId,
                        FromAirportId = amm.AirportId,
                        ToAirportId = dxb.AirportId,
                        DepartureDateTime = DateTime.Today.AddDays(5).AddHours(8).AddMinutes(30),
                        ArrivalDateTime = DateTime.Today.AddDays(5).AddHours(11).AddMinutes(50),
                        Price = 121.00m,
                        AvailableSeats = 25,
                        AircraftType = "Boeing 737-800"
                    },
                    new Flight
                    {
                        FlightNumber = "EK 903",
                        AirlineId = emirates.AirlineId,
                        FromAirportId = amm.AirportId,
                        ToAirportId = ist.AirportId,
                        DepartureDateTime = DateTime.Today.AddDays(7).AddHours(9).AddMinutes(10),
                        ArrivalDateTime = DateTime.Today.AddDays(7).AddHours(12).AddMinutes(15),
                        Price = 210.00m,
                        AvailableSeats = 30,
                        AircraftType = "Boeing 777"
                    },
                    new Flight
                    {
                        FlightNumber = "RJ 612",
                        AirlineId = royalJordanian.AirlineId,
                        FromAirportId = amm.AirportId,
                        ToAirportId = dxb.AirportId,
                        DepartureDateTime = DateTime.Today.AddDays(10).AddHours(14).AddMinutes(20),
                        ArrivalDateTime = DateTime.Today.AddDays(10).AddHours(17).AddMinutes(50),
                        Price = 180.00m,
                        AvailableSeats = 20,
                        AircraftType = "Airbus A320"
                    },
                    new Flight
                    {
                        FlightNumber = "QR 310",
                        AirlineId = qatarAirways.AirlineId,
                        FromAirportId = amm.AirportId,
                        ToAirportId = doh.AirportId,
                        DepartureDateTime = DateTime.Today.AddDays(12).AddHours(22).AddMinutes(30),
                        ArrivalDateTime = DateTime.Today.AddDays(13).AddHours(1).AddMinutes(30),
                        Price = 190.00m,
                        AvailableSeats = 18,
                        AircraftType = "Airbus A350"
                    }
                );

                context.SaveChanges();
            }
        }

        private static void EnsureUserSecurity(ApplicationDbContext context)
        {
            var users = context.Users.ToList();
            var changed = false;

            foreach (var user in users)
            {
                if (string.IsNullOrWhiteSpace(user.Role))
                {
                    user.Role = "User";
                    changed = true;
                }

                if (!PasswordHelper.IsHashedPassword(user.Password))
                {
                    user.Password = PasswordHelper.HashPassword(user.Password);
                    changed = true;
                }
            }

            const string adminEmail = "admin@flybook.com";
            var admin = context.Users
                .FirstOrDefault(u => u.Email.ToLower() == adminEmail);

            if (admin == null)
            {
                context.Users.Add(new User
                {
                    FullName = "FlyBook Admin",
                    Email = adminEmail,
                    Password = PasswordHelper.HashPassword("Admin123!"),
                    Role = "Admin",
                    IsActive = true
                });

                changed = true;
            }
            else
            {
                if (admin.Role != "Admin")
                {
                    admin.Role = "Admin";
                    changed = true;
                }

                if (!admin.IsActive)
                {
                    admin.IsActive = true;
                    changed = true;
                }

                if (!PasswordHelper.IsHashedPassword(admin.Password))
                {
                    admin.Password = PasswordHelper.HashPassword(admin.Password);
                    changed = true;
                }
            }

            if (changed)
            {
                context.SaveChanges();
            }
        }
    }
}
