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
            var missingPriorityMeetings = schedule
                .NotUsedMeetings
                .Count(m => m.Discipline.Name == "Физкультура" || m.Discipline.Name == "ИнЯз");
            return -1 * missingPriorityMeetings;
        }
    }
}