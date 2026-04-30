namespace RideRotationApp2.Models;

public class NextShiftMove
{
    public string EmployeeName { get; set; } = "";
    public string FromPosition { get; set; } = "";
    public string ToPosition { get; set; } = "";
    public bool IsTraining { get; set; }
}