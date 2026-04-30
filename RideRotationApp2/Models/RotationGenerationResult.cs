namespace RideRotationApp2.Models;

public class RotationGenerationResult
{
    public List<RotationResultItem> Assignments { get; set; } = new();
    public List<Employee> Breakers { get; set; } = new();
    public Dictionary<int, List<Ride>> BreakerCoverage { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}