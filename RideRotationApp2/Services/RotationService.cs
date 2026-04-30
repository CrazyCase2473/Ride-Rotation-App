using Microsoft.EntityFrameworkCore;
using RideRotationApp2.Data;
using RideRotationApp2.Models;

namespace RideRotationApp2.Services;

public class RotationService
{
    private readonly AppDbContext _db;

    public RotationService(AppDbContext db)
    {
        _db = db;
    }

    public Dictionary<Ride, int> GetCertificationCounts(List<Employee> employees, List<Ride> rides)
    {
        return rides.ToDictionary(
            ride => ride,
            ride => employees.Count(employee => IsCertified(employee, ride))
        );
    }

    public async Task<RotationGenerationResult> GenerateRotation(
        List<Employee> employees,
        List<Ride> rides,
        int trainingCount,
        int breakerCount,
        List<int> cannotBeBreakerEmployeeIds)
    {
        var result = new RotationGenerationResult();

        var recentAssignments = await _db.RotationAssignments
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync();

        var normalAssignments = GenerateNormalAssignmentsWithBacktracking(
            employees,
            rides,
            recentAssignments);

        if (normalAssignments.Count != rides.Count)
        {
            result.Warnings.Add("Could not find a complete rotation for all selected rides.");
            return result;
        }

        result.Assignments.AddRange(normalAssignments);

        int trainingsAdded = TryAddTrainings(
            result.Assignments,
            employees,
            rides,
            trainingCount);

        if (trainingsAdded < trainingCount)
        {
            result.Warnings.Add($"Only added {trainingsAdded} out of {trainingCount} trainings.");
        }

        AddBreakers(
            result,
            employees,
            rides,
            breakerCount,
            cannotBeBreakerEmployeeIds);

        return result;
    }

    private List<RotationResultItem> GenerateNormalAssignmentsWithBacktracking(
        List<Employee> employees,
        List<Ride> rides,
        List<RotationAssignment> recentAssignments)
    {
        var assignments = new List<RotationResultItem>();
        var usedEmployeeIds = new HashSet<int>();

        var ridesByDifficulty = rides
            .OrderBy(ride => employees.Count(employee => IsCertified(employee, ride)))
            .ToList();

        bool success = TryAssignRide(
            0,
            ridesByDifficulty,
            employees,
            recentAssignments,
            assignments,
            usedEmployeeIds);

        if (!success)
        {
            return new List<RotationResultItem>();
        }

        return assignments;
    }

    private bool TryAssignRide(
        int rideIndex,
        List<Ride> rides,
        List<Employee> employees,
        List<RotationAssignment> recentAssignments,
        List<RotationResultItem> assignments,
        HashSet<int> usedEmployeeIds)
    {
        if (rideIndex >= rides.Count)
        {
            return true;
        }

        var ride = rides[rideIndex];

        var candidates = employees
            .Where(employee =>
                !usedEmployeeIds.Contains(employee.Id) &&
                IsCertified(employee, ride))
            .OrderBy(employee => RecentRidePenalty(employee, ride, recentAssignments))
            .ThenBy(employee => employee.Certifications.Count(c => c.IsCertified))
            .ToList();

        foreach (var candidate in candidates)
        {
            assignments.Add(new RotationResultItem
            {
                Ride = ride,
                Employee = candidate,
                IsTraining = false
            });

            usedEmployeeIds.Add(candidate.Id);

            bool worked = TryAssignRide(
                rideIndex + 1,
                rides,
                employees,
                recentAssignments,
                assignments,
                usedEmployeeIds);

            if (worked)
            {
                return true;
            }

            assignments.RemoveAt(assignments.Count - 1);
            usedEmployeeIds.Remove(candidate.Id);
        }

        return false;
    }

    private int RecentRidePenalty(
        Employee employee,
        Ride ride,
        List<RotationAssignment> recentAssignments)
    {
        const int memoryCount = 3;

        var recentRideIds = recentAssignments
            .Where(a => a.EmployeeId == employee.Id)
            .Take(memoryCount)
            .Select(a => a.RideId)
            .ToList();

        return recentRideIds.Contains(ride.Id) ? 10 : 0;
    }

    private int TryAddTrainings(
        List<RotationResultItem> assignments,
        List<Employee> employees,
        List<Ride> rides,
        int trainingCount)
    {
        int trainingsAdded = 0;

        var employeesByTrainingNeed = employees
            .OrderBy(employee => employee.Certifications.Count(c => c.IsCertified))
            .ToList();

        foreach (var trainee in employeesByTrainingNeed)
        {
            if (trainingsAdded >= trainingCount)
                break;

            var traineeCurrentAssignment = assignments
                .FirstOrDefault(a => a.Employee.Id == trainee.Id && !a.IsTraining);

            if (traineeCurrentAssignment == null)
                continue;

            var possibleTrainingRides = rides
                .Where(ride => !IsCertified(trainee, ride))
                .Where(ride => CanTrainOnRide(trainee, ride))
                .OrderBy(ride => employees.Count(employee => IsCertified(employee, ride)))
                .ToList();

            foreach (var trainingRide in possibleTrainingRides)
            {
                var personCurrentlyOnTrainingRide = assignments
                    .FirstOrDefault(a => a.Ride.Id == trainingRide.Id && !a.IsTraining);

                if (personCurrentlyOnTrainingRide == null)
                    continue;

                var traineeOldRide = traineeCurrentAssignment.Ride;

                if (!IsCertified(personCurrentlyOnTrainingRide.Employee, traineeOldRide))
                    continue;

                traineeCurrentAssignment.Ride = trainingRide;
                traineeCurrentAssignment.IsTraining = true;

                personCurrentlyOnTrainingRide.Ride = traineeOldRide;
                personCurrentlyOnTrainingRide.IsTraining = false;

                trainingsAdded++;
                break;
            }
        }

        return trainingsAdded;
    }

    private void AddBreakers(
        RotationGenerationResult result,
        List<Employee> employees,
        List<Ride> rides,
        int breakerCount,
        List<int> cannotBeBreakerEmployeeIds)
    {
        if (breakerCount <= 0)
            return;

        var assignedEmployeeIds = result.Assignments
            .Select(a => a.Employee.Id)
            .ToHashSet();

        var breakerCandidates = employees
            .Where(e =>
                !assignedEmployeeIds.Contains(e.Id) &&
                !cannotBeBreakerEmployeeIds.Contains(e.Id))
            .OrderByDescending(e => rides.Count(r => IsCertified(e, r)))
            .ToList();

        result.Breakers = breakerCandidates
            .Take(breakerCount)
            .ToList();

        if (result.Breakers.Count < breakerCount)
        {
            result.Warnings.Add($"Only added {result.Breakers.Count} out of {breakerCount} breakers. Ride staffing was prioritized.");
        }

        foreach (var breaker in result.Breakers)
        {
            result.BreakerCoverage[breaker.Id] = rides
                .Where(ride => IsCertified(breaker, ride))
                .ToList();
        }

        var coveredRideIds = result.BreakerCoverage
            .SelectMany(pair => pair.Value)
            .Select(ride => ride.Id)
            .ToHashSet();

        foreach (var ride in rides)
        {
            if (!coveredRideIds.Contains(ride.Id))
            {
                result.Warnings.Add($"No breaker covers {ride.Name}.");
            }
        }
    }

    public async Task SaveRotation(List<RotationResultItem> assignments)
    {
        int rotationNumber = await _db.RotationAssignments
            .Select(r => r.RotationNumber)
            .DefaultIfEmpty(0)
            .MaxAsync() + 1;

        foreach (var item in assignments.Where(a => !a.IsTraining))
        {
            _db.RotationAssignments.Add(new RotationAssignment
            {
                EmployeeId = item.Employee.Id,
                RideId = item.Ride.Id,
                RotationNumber = rotationNumber,
                AssignedAt = DateTime.Now
            });
        }

        await _db.SaveChangesAsync();
    }

    private bool CanTrainOnRide(Employee employee, Ride ride)
    {
        if (ride.PrerequisiteRideId == null)
            return true;

        return employee.Certifications.Any(cert =>
            cert.RideId == ride.PrerequisiteRideId &&
            cert.IsCertified);
    }

    private bool IsCertified(Employee employee, Ride ride)
    {
        return employee.Certifications.Any(cert =>
            cert.RideId == ride.Id &&
            cert.IsCertified);
    }
}