namespace RideRotationApp2.Models;

public class NextShiftResult
{
    public bool Success { get; set; }
    public List<NextShiftMove> Moves { get; set; } = new();
    public ShiftSnapshot? StartingSnapshot { get; set; }
    public ShiftSnapshot Snapshot { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
