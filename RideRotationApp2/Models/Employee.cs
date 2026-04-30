namespace RideRotationApp2.Models;

public class Employee
{
    public int Id { get; set; }

    public string Name { get; set; } = "";
    public string Area { get; set; } = "";

    public List<EmployeeRideCertification> Certifications { get; set; } = new();
}