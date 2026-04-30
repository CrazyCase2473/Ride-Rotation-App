namespace RideRotationApp2.Models;

public class RotationResultItem
{
    public Ride Ride { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
    public bool IsTraining { get; set; }
}