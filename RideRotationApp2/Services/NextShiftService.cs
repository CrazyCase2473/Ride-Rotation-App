using RideRotationApp2.Models;

namespace RideRotationApp2.Services;

public class NextShiftService
{
    public NextShiftResult GenerateNextShift(
        RotationGenerationResult currentResult,
        List<Employee> employees,
        List<Ride> rides,
        List<ShiftSnapshot> shiftSnapshots,
        HashSet<int> activeEmployeeIds,
        List<TrainingRequest> trainingRequests)
    {
        var result = new NextShiftResult();

        if (currentResult.Assignments.Count == 0)
        {
            result.Warnings.Add("Generate a rotation first.");
            return result;
        }

        ShiftSnapshot currentSnapshot;

        if (shiftSnapshots.Count == 0)
        {
            currentSnapshot = BuildSnapshotFromCurrentRotation(currentResult);
            result.StartingSnapshot = currentSnapshot;
        }
        else
        {
            currentSnapshot = shiftSnapshots.Last();
        }

        var ridePositions = currentResult.Assignments
            .Select(a => a.Ride.Name)
            .Distinct()
            .ToList();

        var breakerPositions = currentResult.Breakers
            .Select((breaker, index) => $"Breaker {index + 1}")
            .ToList();

        var possibleLineSets = BuildPossibleAutomaticRotationLineSets(ridePositions, breakerPositions);

        foreach (var lineSet in possibleLineSets)
        {
            var allMoves = new List<NextShiftMove>();
            var nextSnapshot = new ShiftSnapshot();
            var availableTrainingRequests = trainingRequests.ToList();

            bool lineSetWorked = true;

            foreach (var line in lineSet)
            {
                bool lineWorked = TryBuildBestLineMoves(
                    line,
                    currentSnapshot,
                    nextSnapshot,
                    allMoves,
                    availableTrainingRequests,
                    employees,
                    rides,
                    activeEmployeeIds);

                if (!lineWorked)
                {
                    lineSetWorked = false;
                    break;
                }
            }

            if (!lineSetWorked)
                continue;

            if (!SpecificTrainingRequestsWereMet(allMoves, trainingRequests, employees, rides))
                continue;

            result.Success = true;
            result.Moves = allMoves;
            result.Snapshot = nextSnapshot;
            return result;
        }

        result.Warnings.Add("Could not create a valid next shift with the current breaker setup, certifications, and training requests.");
        return result;
    }

    private bool SpecificTrainingRequestsWereMet(
        List<NextShiftMove> moves,
        List<TrainingRequest> requests,
        List<Employee> employees,
        List<Ride> rides)
    {
        foreach (var request in requests)
        {
            if (request.EmployeeId == null && request.RideId == null)
                continue;

            var requestedEmployee = request.EmployeeId == null
                ? null
                : employees.FirstOrDefault(e => e.Id == request.EmployeeId);

            var requestedRide = request.RideId == null
                ? null
                : rides.FirstOrDefault(r => r.Id == request.RideId);

            bool matched = moves.Any(move =>
                move.IsTraining &&
                (requestedEmployee == null || move.EmployeeName.Equals(requestedEmployee.Name, StringComparison.OrdinalIgnoreCase)) &&
                (requestedRide == null || move.ToPosition.Equals(requestedRide.Name, StringComparison.OrdinalIgnoreCase)));

            if (!matched)
                return false;
        }

        return true;
    }

    private List<List<List<string>>> BuildPossibleAutomaticRotationLineSets(
        List<string> ridePositions,
        List<string> breakerPositions)
    {
        var lineSets = new List<List<List<string>>>();

        if (ridePositions.Count == 0)
            return lineSets;

        if (breakerPositions.Count == 0)
        {
            lineSets.Add(new List<List<string>> { ridePositions.ToList() });
            return lineSets;
        }

        if (breakerPositions.Count == 1)
        {
            lineSets.Add(new List<List<string>>
            {
                new List<string> { breakerPositions[0] }
                    .Concat(ridePositions)
                    .ToList()
            });

            return lineSets;
        }

        if (breakerPositions.Count == 2)
        {
            foreach (var firstGroup in GetRideGroupsNearTargetSize(ridePositions, 4))
            {
                var secondGroup = ridePositions
                    .Where(r => !firstGroup.Contains(r))
                    .ToList();

                if (secondGroup.Count == 0)
                    continue;

                lineSets.Add(new List<List<string>>
                {
                    new List<string> { breakerPositions[0] }.Concat(firstGroup).ToList(),
                    new List<string> { breakerPositions[1] }.Concat(secondGroup).ToList()
                });

                lineSets.Add(new List<List<string>>
                {
                    new List<string> { breakerPositions[0] }.Concat(secondGroup).ToList(),
                    new List<string> { breakerPositions[1] }.Concat(firstGroup).ToList()
                });
            }

            return lineSets;
        }

        var groups = new List<List<string>>();

        for (int i = 0; i < breakerPositions.Count; i++)
            groups.Add(new List<string> { breakerPositions[i] });

        for (int i = 0; i < ridePositions.Count; i++)
            groups[i % breakerPositions.Count].Add(ridePositions[i]);

        lineSets.Add(groups);
        return lineSets;
    }

    private List<List<string>> GetRideGroupsNearTargetSize(List<string> ridesToSplit, int targetSize)
    {
        var groups = new List<List<string>>();

        int minSize = Math.Max(1, targetSize - 1);
        int maxSize = Math.Min(ridesToSplit.Count - 1, targetSize + 1);

        for (int size = minSize; size <= maxSize; size++)
        {
            BuildCombinations(ridesToSplit, size, 0, new List<string>(), groups);
        }

        return groups
            .OrderBy(g => Math.Abs(g.Count - targetSize))
            .ThenBy(g => string.Join(",", g))
            .ToList();
    }

    private void BuildCombinations(
        List<string> source,
        int size,
        int startIndex,
        List<string> current,
        List<List<string>> results)
    {
        if (current.Count == size)
        {
            results.Add(current.ToList());
            return;
        }

        for (int i = startIndex; i < source.Count; i++)
        {
            current.Add(source[i]);
            BuildCombinations(source, size, i + 1, current, results);
            current.RemoveAt(current.Count - 1);
        }
    }

    private bool TryBuildBestLineMoves(
        List<string> linePositions,
        ShiftSnapshot currentSnapshot,
        ShiftSnapshot nextSnapshot,
        List<NextShiftMove> allMoves,
        List<TrainingRequest> availableTrainingRequests,
        List<Employee> employees,
        List<Ride> rides,
        HashSet<int> activeEmployeeIds)
    {
        var positionsInLine = linePositions
            .Where(p => currentSnapshot.Positions.ContainsKey(p))
            .ToList();

        if (positionsInLine.Count < 2)
            return false;

        var possibleOrders = GetPositionPermutations(positionsInLine, maxResults: 5000);

        foreach (var order in possibleOrders)
        {
            var testMoves = new List<NextShiftMove>();

            var testSnapshot = new ShiftSnapshot
            {
                Positions = new Dictionary<string, string>(nextSnapshot.Positions),
                TrainingStatus = new Dictionary<string, bool>(nextSnapshot.TrainingStatus)
            };

            var testAvailableRequests = availableTrainingRequests.ToList();

            bool worked = true;

            for (int i = 0; i < order.Count; i++)
            {
                string fromPosition = order[i];
                string toPosition = order[(i + 1) % order.Count];

                string employeeName = currentSnapshot.Positions[fromPosition];

                var employee = employees.FirstOrDefault(e =>
                    e.Name.Equals(employeeName, StringComparison.OrdinalIgnoreCase));

                if (employee == null || !activeEmployeeIds.Contains(employee.Id))
                {
                    worked = false;
                    break;
                }

                bool isTraining = false;

                if (!CanMoveToPosition(
                    employee,
                    toPosition,
                    testAvailableRequests,
                    rides,
                    out isTraining,
                    out var usedRequest))
                {
                    worked = false;
                    break;
                }

                if (usedRequest != null)
                    testAvailableRequests.Remove(usedRequest);

                testMoves.Add(new NextShiftMove
                {
                    EmployeeName = employee.Name,
                    FromPosition = fromPosition,
                    ToPosition = toPosition,
                    IsTraining = isTraining
                });

                testSnapshot.Positions[toPosition] = employee.Name;
                testSnapshot.TrainingStatus[toPosition] = isTraining;
            }

            if (worked)
            {
                availableTrainingRequests.Clear();
                availableTrainingRequests.AddRange(testAvailableRequests);

                allMoves.AddRange(testMoves);

                foreach (var pair in testSnapshot.Positions)
                    nextSnapshot.Positions[pair.Key] = pair.Value;

                foreach (var pair in testSnapshot.TrainingStatus)
                    nextSnapshot.TrainingStatus[pair.Key] = pair.Value;

                return true;
            }
        }

        return false;
    }

    private List<List<string>> GetPositionPermutations(List<string> positions, int maxResults)
    {
        var results = new List<List<string>>();

        void Backtrack(List<string> current, List<string> remaining)
        {
            if (results.Count >= maxResults)
                return;

            if (remaining.Count == 0)
            {
                results.Add(current.ToList());
                return;
            }

            foreach (var position in remaining.ToList())
            {
                current.Add(position);

                var nextRemaining = remaining
                    .Where(p => p != position)
                    .ToList();

                Backtrack(current, nextRemaining);

                current.RemoveAt(current.Count - 1);
            }
        }

        Backtrack(new List<string>(), positions);

        return results;
    }

    private bool CanMoveToPosition(
        Employee employee,
        string toPosition,
        List<TrainingRequest> availableTrainingRequests,
        List<Ride> rides,
        out bool isTraining,
        out TrainingRequest? usedTrainingRequest)
    {
        isTraining = false;
        usedTrainingRequest = null;

        if (IsBreakerPosition(toPosition))
            return true;

        var ride = rides.FirstOrDefault(r =>
            r.Name.Equals(toPosition, StringComparison.OrdinalIgnoreCase));

        if (ride == null)
            return false;

        if (IsCertified(employee, ride))
            return true;

        var matchingRequest = availableTrainingRequests.FirstOrDefault(request =>
            (request.EmployeeId == null || request.EmployeeId == employee.Id) &&
            (request.RideId == null || request.RideId == ride.Id));

        if (matchingRequest != null && CanTrainOnRide(employee, ride))
        {
            isTraining = true;
            usedTrainingRequest = matchingRequest;
            return true;
        }

        return false;
    }

    private ShiftSnapshot BuildSnapshotFromCurrentRotation(RotationGenerationResult currentResult)
    {
        var snapshot = new ShiftSnapshot();

        foreach (var assignment in currentResult.Assignments)
        {
            snapshot.Positions[assignment.Ride.Name] = assignment.Employee.Name;
            snapshot.TrainingStatus[assignment.Ride.Name] = assignment.IsTraining;
        }

        for (int i = 0; i < currentResult.Breakers.Count; i++)
        {
            string breakerPosition = $"Breaker {i + 1}";
            snapshot.Positions[breakerPosition] = currentResult.Breakers[i].Name;
            snapshot.TrainingStatus[breakerPosition] = false;
        }

        return snapshot;
    }

    private bool IsBreakerPosition(string position)
    {
        return position.StartsWith("Breaker", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsCertified(Employee employee, Ride ride)
    {
        return employee.Certifications.Any(cert =>
            cert.RideId == ride.Id &&
            cert.IsCertified);
    }

    private bool CanTrainOnRide(Employee employee, Ride ride)
    {
        if (ride.PrerequisiteRideId == null)
            return true;

        return employee.Certifications.Any(cert =>
            cert.RideId == ride.PrerequisiteRideId &&
            cert.IsCertified);
    }
}