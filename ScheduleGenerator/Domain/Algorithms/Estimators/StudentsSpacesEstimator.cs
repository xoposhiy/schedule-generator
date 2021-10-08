using System;

namespace Domain.Algorithms.Estimators
{
    public class StudentsSpacesEstimator : IEstimator
    {
        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            throw new NotImplementedException();
        }

        public double Estimate(Schedule schedule)
        {
            //TODO придумать как учитывать пары, которые идут не весь семестр.
            //Например, учитывать аналогично четным-нечетным неделям (см ниже).
            var penalty = 0;

            foreach (var byGroup in schedule.GroupMeetingsByTime.Values)
            foreach (var byWeekType in byGroup.Values)
                penalty += byWeekType.GetMeetingsGapCount();

            return -penalty;
        }
    }
}