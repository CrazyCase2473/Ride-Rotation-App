# Ride Rotation App

This is a web application to help team leads manage ride certifications, staffing, and daily ride rotations more efficiently.

This project was started because of real operational challenges I had as a team lead at Frontier City and is designed to help staff employees fairly while ensuring all rides remain staffed properly.

This Web Application is still in development
## Features

### Certification Management
- Stores employee ride certifications
- Edit certificatins through the UI
- Add and remove employees
- Add and remove rides
- Filter employees by ride or area

### Rotation Generation
- Automatically generates ride rotations
- Prevents employees from stay at the same ride repeatedly
- Remembers recent ride placement
- Supports breaker positions
- Handle staffing requirements for open rides
- Warns when a ride cannot be staffed

### Training Support
- Assign training positions during rotation
- Pioritizes training opportunities
- Prevents invalid training combinations

## Built With
 - C#
 - ASP.NET Core Blazor Server
 - Entity Framework Core
 - SQLite
 - Razor Components

## Future Plans
 - Drag and drop rotation editing
 - Multiple area support
 - Shift history tracking
