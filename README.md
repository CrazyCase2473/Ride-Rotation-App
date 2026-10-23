# Ride Rotation App

A web app that helps team leads manage ride certifications, staffing, and daily ride rotations.

I started this project because of real problems I ran into as a team lead at Frontier City. It's built to rotate employees fairly while making sure every ride stays properly staffed.

> **Status:** still in development.

## Features

### Certification Management
- Store which rides each employee is certified on
- Edit certifications right from the certification grid
- Add and remove employees and rides
- Give employees a home area and rides an area
- Filter employees by area or by ride
- Set ride prerequisites (for example, an employee must know a loader position before learning a panel)

### Rotation Generation
- Automatically generate ride rotations for each area
- Keep employees from landing on the same ride over and over by remembering recent placements
- Assign breakers and show which rides each breaker covers
- Mark employees who can't be breakers
- Choose which employees and rides are active for the shift
- Warn you when a ride can't be staffed

### Next Shift Rotations
- Build the next shift's rotation from the current one
- Backtrack to find a valid arrangement when the first attempt doesn't work
- See who moves where from one shift to the next
- Look back at previous rotations

### Training Support
- Choose who to train and which ride to train them on
- Training requests are filled first, before the rest of the rotation
- Block training that doesn't make sense (already certified, or missing a prerequisite)

## Built With
- C# / .NET 8
- ASP.NET Core Blazor Server (Razor Components)
- Entity Framework Core
- SQLite

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- The EF Core CLI tool: `dotnet tool install --global dotnet-ef`

### Run it locally
```bash
git clone https://github.com/CrazyCase2473/Ride-Rotation-App.git
cd Ride-Rotation-App/RideRotationApp2
dotnet ef database update
dotnet run
```

`dotnet ef database update` creates the SQLite database at `Data/ride_rotation.db`. The database is not checked into the repo.

Then open the URL shown in the terminal (by default `http://localhost:5120`) and go to **Certifications** to add employees and rides, then **Rotation** to build a rotation.

## Project Structure
```
RideRotationApp2/
├── Components/Pages/   # Certifications and Rotation pages
├── Data/               # EF Core database context
├── Migrations/         # Database migrations
├── Models/             # Employees, rides, certifications, rotations
└── Services/           # Rotation, next shift, and certification logic
```

## Future Plans
- Drag and drop rotation editing
- Shift history tracking
