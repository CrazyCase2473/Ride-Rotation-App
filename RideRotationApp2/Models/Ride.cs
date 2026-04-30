namespace RideRotationApp2.Models;

public class Ride
{
    public int Id { get; set; }

    public string Name { get; set; } = "";
    public string Area { get; set; } = "";

    public int? PrerequisiteRideId { get; set; }
    public Ride? PrerequisiteRide { get; set; }

    public List<EmployeeRideCertification> Certifications { get; set; } = new();
}