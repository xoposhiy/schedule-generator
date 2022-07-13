namespace Domain2.Algorithms.Estimators.StudentEstimators;

public class LocationPerDayEstimator : IStudentEstimator
{
    //TODO С четностью разобраться
    public double EstimateStudentSchedule(List<Meeting2> meetings)
    {
        var maxPenalty = meetings.Count - 1;
        return (double) meetings
                   .GroupBy(m => m.MeetingTime!.DayOfWeek)
                   .Sum(g => GetPenalty(g.Select(m => m.Place)))
               / maxPenalty;
    }
    
    private static int GetPenalty(IEnumerable<string> meetingsLocations)
    {
        var locations = meetingsLocations.Where(l => l != "").ToList();
        var count = 0;
        if (locations.Count == 0) return 0;
        var previous = locations[0];
        for (var i = 1; i < locations.Count; i++)
        {
            if (previous == locations[i]) continue;
            previous = locations[i];
            count++;
        }

        return count;
    }
}