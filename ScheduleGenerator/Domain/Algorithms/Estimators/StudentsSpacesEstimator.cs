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
            var penalty = 0d;

            var groups = schedule.GroupMeetingsByTime.Values;
            foreach (var byGroup in groups)
            foreach (var byWeekType in byGroup.Values)
            foreach (var byDay in byWeekType.Values)
            {
                penalty += byDay.GetMeetingsSpacesCount();
            }

            return -penalty / (groups.Count * 2);
        }
    }
}