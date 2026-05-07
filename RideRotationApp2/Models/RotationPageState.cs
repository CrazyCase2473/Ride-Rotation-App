using RideRotationApp2.Models;

namespace RideRotationApp2.Models;

public class RotationPageState
{
    public Dictionary<int, bool> SelectedEmployeeIds { get; set; } = new();
    public Dictionary<int, bool> SelectedRideIds { get; set; } = new();
    public Dictionary<int, bool> CannotBeBreakerIds { get; set; } = new();

    public List<TrainingRequest> TrainingRequests { get; set; } = new();
    public List<TrainingRequest> NextShiftTrainingRequests { get; set; } = new();

    public void Initialize(List<Employee> employees, List<Ride> rides)
    {
        foreach (var employee in employees)
        {
            SelectedEmployeeIds[employee.Id] = true;
            CannotBeBreakerIds[employee.Id] = false;
        }

        foreach (var ride in rides)
        {
            SelectedRideIds[ride.Id] = true;
        }
    }

    public void SyncTrainingRequests(int trainingCount, int nextShiftTrainingCount)
    {
        while (TrainingRequests.Count < trainingCount)
            TrainingRequests.Add(new TrainingRequest());

        while (TrainingRequests.Count > trainingCount)
            TrainingRequests.RemoveAt(TrainingRequests.Count - 1);

        while (NextShiftTrainingRequests.Count < nextShiftTrainingCount)
            NextShiftTrainingRequests.Add(new TrainingRequest());

        while (NextShiftTrainingRequests.Count > nextShiftTrainingCount)
            NextShiftTrainingRequests.RemoveAt(NextShiftTrainingRequests.Count - 1);
    }

    public List<Employee> GetSelectedEmployees(List<Employee> employees)
    {
        return employees
            .Where(e => SelectedEmployeeIds.GetValueOrDefault(e.Id))
            .ToList();
    }

    public List<Ride> GetSelectedRides(List<Ride> rides)
    {
        return rides
            .Where(r => SelectedRideIds.GetValueOrDefault(r.Id))
            .ToList();
    }

    public List<int> GetCannotBeBreakerEmployeeIds()
    {
        return CannotBeBreakerIds
            .Where(pair => pair.Value)
            .Select(pair => pair.Key)
            .ToList();
    }

    public HashSet<int> GetActiveEmployeeIds()
    {
        return SelectedEmployeeIds
            .Where(pair => pair.Value)
            .Select(pair => pair.Key)
            .ToHashSet();
    }
}