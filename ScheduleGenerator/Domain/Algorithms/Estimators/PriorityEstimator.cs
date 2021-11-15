using Infrastructure;

namespace Domain.Algorithms.Estimators
{
    public abstract class PriorityEstimator : IEstimator
    {
        public const double AndreyConstant = 5;
        // private const double DanyaConstant = Math.E * 100;

        public double EstimateMeetingToAdd(Schedule schedule, Meeting meetingToAdd)
        {
            var maxPenalty = schedule.Meetings.Count + schedule.NotUsedMeetings.Count;
            var penaltyDelta = 0d;

            foreach (var linkedMeeting in meetingToAdd.GetLinkedMeetings())
                penaltyDelta += FindPriorityPenalty(linkedMeeting);

            return -penaltyDelta / maxPenalty;
        }

        public double Estimate(Schedule schedule, ILogger? logger = null)
        {
            var penalty = 0d;
            var maxPenalty = schedule.Meetings.Count + schedule.NotUsedMeetings.Count;
            foreach (var meeting in schedule.Meetings)
            {
                var priorityPenalty = FindPriorityPenalty(meeting);
                if (priorityPenalty == 0) continue;
                logger?.Log(GetLogMessage(meeting, priorityPenalty), -priorityPenalty / maxPenalty);
                penalty += priorityPenalty;
            }

            return -penalty / maxPenalty;
        }

        public abstract double FindPriorityPenalty(Meeting meeting);
        public abstract string GetLogMessage(Meeting meeting, double priorityPenalty);
    }
}