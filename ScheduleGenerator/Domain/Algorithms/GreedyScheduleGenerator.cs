using System.Linq;
using Domain.ScheduleLib;

namespace Domain.Algorithms
{
    public class GreedyScheduleGenerator : IScheduleGenerator
    {
        public Schedule MakeSchedule(LearningPlan learningPlan, Requisition requisition)
        {
            var meetingsToFill =
                Conversions.RequistionToMeetingConverter.ConvertRequistionToMeetingWithoutTime(requisition).ToArray();
            var bestSchedule = new Schedule(new Meeting[0]);
            var bestPenalty = double.PositiveInfinity;
            for (var i = 0; i < 100; ++i)
            {
                var shuffledMeetingsToFill = meetingsToFill.Shuffled();
                var tryingResult = TryMakeSchedule(learningPlan, shuffledMeetingsToFill, requisition);
                if (bestPenalty > tryingResult.Item2)
                {
                    bestPenalty = tryingResult.Item2;
                    bestSchedule = tryingResult.Item1;
                }
            }

            return bestSchedule;
        }

        public (Schedule, double) TryMakeSchedule(LearningPlan learningPlan, Meeting[] meetingsToFill, Requisition requisition)
        {
            return (new Schedule(new Meeting[0]), 0);
        }
    }
}
