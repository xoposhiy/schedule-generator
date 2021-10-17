using System.Linq;

namespace Domain.Algorithms.Estimators
{
    public class PriorityMeetingsEstimator : IEstimator
    {
        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            throw new System.NotImplementedException();
        }

        public double Estimate(Schedule schedule)
        {
            return -schedule
                .NotUsedMeetings.Select(m => m.Discipline.Name)
                .Count(n => n is "Физкультура" or "ИнЯз");
        }
    }
}