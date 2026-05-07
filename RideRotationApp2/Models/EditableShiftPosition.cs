namespace RideRotationApp2.Models;

public class EditableShiftPosition
{
    public string PositionName { get; set; } = "";
    public int? EmployeeId { get; set; }
    public bool IsTraining { get; set; }
}