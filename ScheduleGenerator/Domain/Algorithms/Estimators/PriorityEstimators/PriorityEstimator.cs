using Infrastructure;

namespace Domain.Algorithms.Estimators.PriorityEstimators
{
    public abstract class PriorityEstimator : IEstimator
    {
        protected const double AndreyConstant = 5;
        // private const double DanyaConstant = Math.E * 100;

        public double EstimateMeetingToAdd(Schedule schedule, Meeting meetingToAdd)
        {
            var penaltyDelta = 0d;
            var maxPenalty = GetMaxPenalty(schedule);

            foreach (var linkedMeeting in meetingToAdd.GetLinkedMeetings())
                penaltyDelta += FindPriorityPenalty(linkedMeeting);

            return -penaltyDelta / maxPenalty;
        }

        public double Estimate(Schedule schedule, ILogger? logger = null)
        {
            var penalty = 0d;
            var maxPenalty = GetMaxPenalty(schedule);
            foreach (var meeting in schedule.Meetings)
            {
                var priorityPenalty = FindPriorityPenalty(meeting);
                if (priorityPenalty == 0) continue;
                logger?.Log(GetLogMessage(meeting, priorityPenalty), -priorityPenalty / maxPenalty);
                penalty += priorityPenalty;
            }

            return -penalty / maxPenalty;
        }

        private static double GetMaxPenalty(Schedule schedule)
        {
            return schedule.Meetings.Count + schedule.NotUsedMeetings.Count;
        }

        protected abstract double FindPriorityPenalty(Meeting meeting);
        protected abstract string GetLogMessage(Meeting meeting, double priorityPenalty);
    }
}