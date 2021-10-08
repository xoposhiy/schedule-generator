using System;

namespace Domain.Algorithms.Estimators
{
    public class TeacherSpacesEstimator : IEstimator
    {
        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            throw new NotImplementedException();
        }

        public double Estimate(Schedule schedule)
        {
            var penalty = 0;

            foreach (var byTeacher in schedule.TeacherMeetingsByTime.Values)
            foreach (var byWeekType in byTeacher.Values)
                penalty += byWeekType.GetMeetingsGapCount();

            return -penalty;
        }
    }
}