using RideRotationApp2.Models;
using System.Text;

namespace RideRotationApp2.Services;

public class CsvService
{
    public List<Employee> ReadCsv(string csvText, out List<Ride> rides)
    {
        var employees = new List<Employee>();
        rides = new List<Ride>();

        var lines = csvText.Split('\n')
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        if (lines.Count == 0)
            return employees;

        var headers = lines[0]
            .Split(',')
            .Select(h => h.Trim())
            .ToList();

        var rideNames = headers.Skip(2).ToList();

        foreach (var rideName in rideNames)
        {
            rides.Add(new Ride
            {
                Name = rideName,
                Area = ""
            });
        }

        foreach (var line in lines.Skip(1))
        {
            var values = line.Split(',')
                .Select(v => v.Trim())
                .ToList();

            var employee = new Employee
            {
                Name = values[0],
                Area = values[1]
            };

            for (int i = 0; i < rides.Count; i++)
            {
                bool isCertified = values[i + 2].ToUpper() == "TRUE";

                employee.Certifications.Add(new EmployeeRideCertification
                {
                    Employee = employee,
                    Ride = rides[i],
                    IsCertified = isCertified
                });
            }

            employees.Add(employee);
        }

        return employees;
    }

    public string BuildCsv(List<Employee> employees, List<Ride> rides)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Name,Area," + string.Join(",", rides.Select(r => r.Name)));

        foreach (var employee in employees)
        {
            var row = new List<string>
            {
                employee.Name,
                employee.Area
            };

            foreach (var ride in rides)
            {
                var certification = employee.Certifications
                    .FirstOrDefault(c => c.Ride.Name == ride.Name);

                row.Add(certification?.IsCertified == true ? "TRUE" : "FALSE");
            }

            sb.AppendLine(string.Join(",", row));
        }

        return sb.ToString();
    }
}