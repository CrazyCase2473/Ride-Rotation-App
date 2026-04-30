namespace RideRotationApp2.Models;

public class ShiftSnapshot
{
    public Dictionary<string, string> Positions { get; set; } = new();
    public Dictionary<string, bool> TrainingStatus { get; set; } = new();
}