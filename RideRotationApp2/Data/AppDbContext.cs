using Microsoft.EntityFrameworkCore;
using RideRotationApp2.Models;

namespace RideRotationApp2.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Ride> Rides => Set<Ride>();
    public DbSet<EmployeeRideCertification> EmployeeRideCertifications => Set<EmployeeRideCertification>();
    public DbSet<RotationAssignment> RotationAssignments => Set<RotationAssignment>();
}