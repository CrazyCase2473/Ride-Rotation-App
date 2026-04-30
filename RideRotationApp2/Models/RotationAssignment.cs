namespace RideRotationApp2.Models;

public class RotationAssignment
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int RideId { get; set; }
    public Ride Ride { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.Now;

    public int RotationNumber { get; set; }
}