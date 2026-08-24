# ✈️ FlyBook — Flight Booking Management System

<p align="center">
  <strong>A modern flight booking web application built with ASP.NET Core MVC, Entity Framework Core, and SQL Server.</strong>
</p>

<p align="center">
  Search flights • Manage bookings • Process payments • Issue tickets • Administer the platform
</p>

---

## 📖 Overview

**FlyBook** is a full-stack flight booking and management system developed using **ASP.NET Core MVC (.NET 10)**. It provides a complete workflow for users to create accounts, search available flights, make reservations, submit passenger information, complete payments, and view issued tickets.

The application also includes a dedicated **Admin Dashboard** for managing flights, airlines, airports, bookings, payments, and user accounts.

FlyBook demonstrates a structured MVC architecture, relational database modeling, Entity Framework Core migrations, session-based authentication, role-based application flows, validation, and automated database seeding.

---

## ✨ Features

### 👤 User Features

- User registration and login
- Secure password hashing
- User profile page
- Active/inactive account status handling
- Session-based authentication
- Browse available flights
- Search flights by:
  - Departure airport
  - Destination airport
  - Departure date
- View detailed flight information
- Create flight bookings
- Add passenger and passport information
- View booking history and booking details
- Complete booking payments
- Generate and view flight tickets
- Log out securely

### 🛫 Flight & Booking Management

- Flight number and aircraft information
- Departure and arrival airports
- Departure and arrival date/time
- Ticket pricing
- Available-seat tracking
- Passenger information management
- Booking status management
- Payment status and payment method tracking
- Unique booking and ticket records

### 🛡️ Admin Dashboard

The administrator has a dedicated management area with the ability to:

- View system dashboard information
- Create, edit, and delete flights
- Create, edit, and delete airlines
- Create, edit, and delete airports
- View all bookings
- View all payments
- View registered users
- Enable or disable user accounts
- Delete users
- View booking tickets

---

## 🧰 Tech Stack

| Technology | Usage |
|---|---|
| **ASP.NET Core MVC** | Web application framework |
| **.NET 10** | Application runtime |
| **C#** | Backend programming language |
| **Entity Framework Core 10** | ORM and database access |
| **SQL Server / LocalDB** | Relational database |
| **Razor Views** | Server-rendered UI |
| **Bootstrap** | Responsive frontend styling |
| **AdminLTE** | Admin dashboard interface |
| **JavaScript / jQuery** | Client-side interactions and validation |
| **ASP.NET Core Session** | User session management |

---

## 🏗️ Project Architecture

FlyBook follows the **Model–View–Controller (MVC)** architectural pattern.

```text
FlyBook/
├── Controllers/
│   ├── AccountController.cs
│   ├── AdminController.cs
│   ├── BookingController.cs
│   ├── FlightsController.cs
│   ├── HomeController.cs
│   └── PaymentController.cs
│
├── Data/
│   ├── ApplicationDbContext.cs
│   └── DbSeeder.cs
│
├── Models/
│   ├── Airline.cs
│   ├── Airport.cs
│   ├── Booking.cs
│   ├── Flight.cs
│   ├── Passenger.cs
│   ├── Payment.cs
│   ├── Ticket.cs
│   └── User.cs
│
├── Services/
│   └── PasswordHelper.cs
│
├── ViewModels/
│   ├── AdminAirlineViewModel.cs
│   ├── AdminAirportViewModel.cs
│   ├── AdminFlightViewModel.cs
│   ├── LoginViewModel.cs
│   ├── PaymentViewModel.cs
│   ├── ProfileViewModel.cs
│   └── RegisterViewModel.cs
│
├── Views/
│   ├── Account/
│   ├── Admin/
│   ├── Booking/
│   ├── Flights/
│   ├── Home/
│   ├── Payment/
│   └── Shared/
│
├── Migrations/
├── wwwroot/
├── Program.cs
├── appsettings.json
└── FlyBook.csproj
```

---

## 🗄️ Database Design

The application uses **Entity Framework Core with SQL Server** and includes the following main entities:

- `User`
- `Airline`
- `Airport`
- `Flight`
- `Passenger`
- `Booking`
- `Payment`
- `Ticket`

### Main Relationships

```text
Airline  ─────< Flight
Airport  ─────< Flight >───── Airport
User     ─────< Booking >──── Flight
Passenger ────< Booking
Booking  ───── Payment
Booking  ───── Ticket
```

The database schema is maintained through **Entity Framework Core Migrations**.

---

## ⚙️ Getting Started

### Prerequisites

Before running FlyBook, make sure you have:

- **.NET 10 SDK**
- **SQL Server** or **SQL Server LocalDB**
- **Visual Studio 2022+** with ASP.NET and web development tools, or another compatible IDE
- **Git**

---

## 🚀 Installation

### 1. Clone the repository

```bash
git clone https://github.com/YOUR-USERNAME/FlyBook.git
cd FlyBook
```

### 2. Restore NuGet packages

```bash
dotnet restore
```

### 3. Configure the database connection

The default configuration uses SQL Server LocalDB:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=FlyBookDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

If you use another SQL Server instance, update `DefaultConnection` inside `appsettings.json`.

### 4. Apply database migrations

The application automatically calls the database migration logic during startup. You can also apply migrations manually:

```bash
dotnet ef database update
```

If the EF Core CLI tool is not installed:

```bash
dotnet tool install --global dotnet-ef
```

### 5. Run the application

```bash
dotnet run --project FlyBook/FlyBook.csproj
```

Alternatively, open the solution in Visual Studio and run the project using **HTTPS**.

---

## 🌱 Seed Data

On first startup, FlyBook automatically seeds the database with sample data, including:

### Airlines

- FlyDubai
- Emirates
- Royal Jordanian
- Qatar Airways

### Airports

- Queen Alia International Airport (`AMM`)
- Dubai International Airport (`DXB`)
- Istanbul Airport (`IST`)
- Hamad International Airport (`DOH`)

### Flights

Several sample flights are generated with future departure dates, available seats, aircraft types, and prices.

---

## 🔐 Default Administrator Account

For local development, the database seeder creates a default administrator account:

```text
Email:    admin@flybook.com
Password: Admin123!
```

> [!WARNING]
> These credentials are intended for development/demo purposes only. Change or remove the default administrator credentials before deploying the application to production.

---

## 🔒 Security Features

FlyBook includes several application security measures:

- Password hashing before passwords are stored
- Session cookies configured as `HttpOnly`
- Anti-forgery token validation for MVC form submissions
- HTTPS redirection
- HSTS outside the development environment
- User account activation/deactivation support
- Separation between regular-user and administrator functionality

For production use, secrets and connection strings should be stored using environment variables, ASP.NET Core User Secrets, or a dedicated secrets manager rather than committed directly to source control.

---

## 🔄 Typical User Flow

```text
Register / Login
      ↓
Search Flights
      ↓
View Flight Details
      ↓
Enter Passenger Information
      ↓
Create Booking
      ↓
Complete Payment
      ↓
View / Receive Ticket
```

---

## 👨‍💼 Typical Admin Flow

```text
Admin Login
     ↓
Admin Dashboard
     ↓
Manage Flights / Airlines / Airports
     ↓
Monitor Bookings & Payments
     ↓
Manage User Accounts
```

---

## 🧪 Development Notes

- The project currently targets `net10.0`.
- Entity Framework migrations are stored in the `Migrations` directory.
- Sample data is created through `Data/DbSeeder.cs`.
- The default database is `FlyBookDB` on SQL Server LocalDB.
- Static assets are served from `wwwroot`.
- Admin UI assets include **AdminLTE**.

---

## 🗺️ Future Improvements

Potential enhancements for future versions include:

- Email booking confirmations
- PDF or downloadable boarding tickets
- Real payment-gateway integration
- Seat-selection interface
- Password reset and email verification
- ASP.NET Core Identity integration
- Two-factor authentication
- Flight filtering and sorting enhancements
- Pagination for administrative tables
- REST API endpoints
- Automated unit and integration tests
- Docker support
- Cloud deployment

---

## 🤝 Contributing

Contributions, suggestions, and improvements are welcome.

1. Fork the repository.
2. Create a feature branch:

```bash
git checkout -b feature/your-feature-name
```

3. Commit your changes:

```bash
git commit -m "Add your feature"
```

4. Push your branch:

```bash
git push origin feature/your-feature-name
```

5. Open a Pull Request.

---

## 📄 License

This project is currently provided for **educational and portfolio purposes**. Add a license file if you plan to distribute or open-source the project under specific terms.

---

## ⭐ Support

If you find FlyBook useful, consider giving the repository a **star ⭐** on GitHub.

<p align="center">
  Built with C#, ASP.NET Core MVC, Entity Framework Core & SQL Server ✈️
</p>
