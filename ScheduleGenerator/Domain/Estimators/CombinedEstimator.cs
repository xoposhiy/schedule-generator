using System.Linq;
using Domain.Conversions;
using Domain.ScheduleLib;

namespace Domain.Estimators
{
    public class CombinedEstimator : IEstimator

    {
        private readonly (IEstimator, int)[] childEstimators;

        public CombinedEstimator(params (IEstimator, int)[] childEstimators)
        {
            this.childEstimators = childEstimators;
        }


        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            return childEstimators.Sum(e => e.Item1.Estimate(schedule, meetingToAdd));
        }

        public double Estimate(Schedule schedule)
        {
            return childEstimators.Sum(e => e.Item1.Estimate(schedule) * e.Item2);
        }
        
        public static CombinedEstimator GetDefaultCombinedEstimator()
        {
            var basic = (new FreedomDegreeEstimator(), 100);
            var groupsSpacesEstimator = (new StudentsSpacesEstimator(), 1);
            var teacherSpacesEstimator = (new TeacherSpacesEstimator(), 1);
            var meetingsPerDayEstimator = (new MeetingsPerDayEstimator(), 1);
            var teacherUsedDaysEstimator = (new TeacherUsedDaysEstimator(), 10);
            var estimator = new CombinedEstimator(basic, groupsSpacesEstimator,
                meetingsPerDayEstimator, teacherSpacesEstimator, teacherUsedDaysEstimator);
            return estimator;
        }
    }
}