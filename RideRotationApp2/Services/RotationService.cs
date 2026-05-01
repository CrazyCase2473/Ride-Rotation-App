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
        List<TrainingRequest> trainingRequests,
        int breakerCount,
        List<int> cannotBeBreakerEmployeeIds)
    {
        var result = new RotationGenerationResult();

        var recentAssignments = await _db.RotationAssignments
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync();

        var forcedTrainingAssignments = new List<RotationResultItem>();
        var usedEmployeeIds = new HashSet<int>();
        var blockedRideIds = new HashSet<int>();

        foreach (var request in trainingRequests)
        {
            if (request.EmployeeId == null || request.RideId == null)
                continue;

            var trainee = employees.FirstOrDefault(e => e.Id == request.EmployeeId);
            var trainingRide = rides.FirstOrDefault(r => r.Id == request.RideId);

            if (trainee == null || trainingRide == null)
            {
                result.Warnings.Add("A requested training could not be found.");
                continue;
            }

            if (IsCertified(trainee, trainingRide))
            {
                result.Warnings.Add($"{trainee.Name} is already certified on {trainingRide.Name}, so they do not need training there.");
                continue;
            }

            if (!CanTrainOnRide(trainee, trainingRide))
            {
                result.Warnings.Add($"{trainee.Name} cannot train on {trainingRide.Name} because they do not meet the prerequisite.");
                continue;
            }

            forcedTrainingAssignments.Add(new RotationResultItem
            {
                Ride = trainingRide,
                Employee = trainee,
                IsTraining = true
            });

            usedEmployeeIds.Add(trainee.Id);
            blockedRideIds.Add(trainingRide.Id);
        }

        var remainingEmployees = employees
            .Where(e => !usedEmployeeIds.Contains(e.Id))
            .ToList();

        var remainingRides = rides
            .Where(r => !blockedRideIds.Contains(r.Id))
            .ToList();

        var normalAssignments = GenerateNormalAssignmentsWithBacktracking(
            remainingEmployees,
            remainingRides,
            recentAssignments);

        if (normalAssignments.Count != remainingRides.Count)
        {
            result.Warnings.Add("Could not build a full rotation around the requested training.");
            return result;
        }

        result.Assignments.AddRange(forcedTrainingAssignments);
        result.Assignments.AddRange(normalAssignments);

        var flexibleRequests = trainingRequests
            .Where(r => r.EmployeeId == null || r.RideId == null)
            .ToList();

        int trainingsAdded = TryAddTrainings(
            result.Assignments,
            employees,
            rides,
            flexibleRequests);

        int totalTrainings = forcedTrainingAssignments.Count + trainingsAdded;

        if (totalTrainings < trainingRequests.Count)
        {
            result.Warnings.Add($"Only added {totalTrainings} out of {trainingRequests.Count} trainings.");
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

        return success ? assignments : new List<RotationResultItem>();
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
            return true;

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

            if (TryAssignRide(
                rideIndex + 1,
                rides,
                employees,
                recentAssignments,
                assignments,
                usedEmployeeIds))
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
        List<TrainingRequest> trainingRequests)
    {
        int trainingsAdded = 0;

        foreach (var request in trainingRequests)
        {
            var possibleTrainees = employees
                .Where(employee =>
                    request.EmployeeId == null ||
                    employee.Id == request.EmployeeId)
                .OrderBy(employee => employee.Certifications.Count(c => c.IsCertified))
                .ToList();

            var possibleTrainingRides = rides
                .Where(ride =>
                    request.RideId == null ||
                    ride.Id == request.RideId)
                .OrderBy(ride => employees.Count(employee => IsCertified(employee, ride)))
                .ToList();

            bool addedThisRequest = false;

            foreach (var trainee in possibleTrainees)
            {
                if (addedThisRequest)
                    break;

                var traineeCurrentAssignment = assignments
                    .FirstOrDefault(a => a.Employee.Id == trainee.Id && !a.IsTraining);

                if (traineeCurrentAssignment == null)
                    continue;

                foreach (var trainingRide in possibleTrainingRides)
                {
                    if (IsCertified(trainee, trainingRide))
                        continue;

                    if (!CanTrainOnRide(trainee, trainingRide))
                        continue;

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
                    addedThisRequest = true;
                    break;
                }
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
        int lastRotationNumber = 0;

        if (await _db.RotationAssignments.AnyAsync())
        {
            lastRotationNumber = await _db.RotationAssignments
                .MaxAsync(r => r.RotationNumber);
        }

        int rotationNumber = lastRotationNumber + 1;

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