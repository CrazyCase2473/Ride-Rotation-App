using Microsoft.EntityFrameworkCore;
using RideRotationApp2.Data;
using RideRotationApp2.Models;

namespace RideRotationApp2.Services;

public class CertificationService
{
    private readonly AppDbContext _db;

    public CertificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(List<Employee>, List<Ride>)> LoadData()
    {
        var rides = await _db.Rides
            .OrderBy(r => r.Name)
            .ToListAsync();

        var employees = await _db.Employees
            .Include(e => e.Certifications)
            .ThenInclude(c => c.Ride)
            .OrderBy(e => e.Name)
            .ToListAsync();

        return (employees, rides);
    }

    public async Task AddEmployee(string name, string area, List<Ride> rides)
    {
        var employee = new Employee
        {
            Name = name,
            Area = area
        };

        foreach (var ride in rides)
        {
            employee.Certifications.Add(new EmployeeRideCertification
            {
                Employee = employee,
                Ride = ride,
                IsCertified = false
            });
        }

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();
    }

    public async Task AddRide(string name, string area, List<Employee> employees)
    {
        var ride = new Ride
        {
            Name = name,
            Area = area
        };

        _db.Rides.Add(ride);

        foreach (var employee in employees)
        {
            _db.EmployeeRideCertifications.Add(new EmployeeRideCertification
            {
                Employee = employee,
                Ride = ride,
                IsCertified = false
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task DeleteEmployee(Employee employee)
    {
        _db.EmployeeRideCertifications.RemoveRange(employee.Certifications);
        _db.Employees.Remove(employee);

        await _db.SaveChangesAsync();
    }

    public async Task DeleteRide(Ride ride)
    {
        var certs = _db.EmployeeRideCertifications
            .Where(c => c.RideId == ride.Id);

        _db.EmployeeRideCertifications.RemoveRange(certs);
        _db.Rides.Remove(ride);

        await _db.SaveChangesAsync();
    }

    public async Task SaveChanges()
    {
        await _db.SaveChangesAsync();
    }
}