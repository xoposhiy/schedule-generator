namespace Domain2.Algorithms.Estimators.StudentEstimators;

public class DayDurationEstimator : IStudentEstimator
{
    //TODO с четностью недель разобраться
    public double EstimateStudentSchedule(List<Meeting2> meetings)
    {
        var maxPenalty = Math.Min(meetings.Count, 5) + GetPenalty(meetings.Count - 5);
        return meetings
                   .GroupBy(m => m.MeetingTime.DayOfWeek)
                   .Sum(g => GetPenalty(g.Count()))
               / maxPenalty;
    }

    private static double GetPenalty(int dayDuration) => 
        dayDuration <= 0 ? 0 : (dayDuration < 2 ? 2 - dayDuration : 0) + (dayDuration > 4 ? dayDuration - 4 : 0);
}