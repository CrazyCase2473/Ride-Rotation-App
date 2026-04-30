namespace RideRotationApp2.Models;

public class EmployeeRideCertification
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int RideId { get; set; }
    public Ride Ride { get; set; } = null!;

    public bool IsCertified { get; set; }
}