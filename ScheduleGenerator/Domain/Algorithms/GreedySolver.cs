using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Domain.Conversions;
using Domain.ScheduleLib;

namespace Domain.Algorithms
{
    public class GreedySolver : ISolver
    {
        private readonly IEstimator estimator;
        private readonly Requisition requisition;
        private readonly Dictionary<string, List<RoomSpec>> classroomsWithSpecs;

        // ReSharper disable once NotAccessedField.Local
        private readonly Random random;

        public GreedySolver(IEstimator estimator, Requisition requisition,
            Dictionary<string, List<RoomSpec>> classroomsWithSpecs, Random random)
        {
            this.estimator = estimator;
            this.requisition = requisition;
            this.classroomsWithSpecs = classroomsWithSpecs;
            this.random = random;
        }

        public IEnumerable<Solution> GetSolution(TimeSpan timeBudget)
        {
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeBudget)
            {
                var currentSchedule = new Schedule(requisition, classroomsWithSpecs);
                var currentScore = double.NegativeInfinity;
                while (true)
                {
                    var meetingsToAdd = currentSchedule.GetMeetingsToAdd()
                        .Select(meetings => (meetings, score: EstimateResult(currentSchedule, meetings)))
                        .OrderByDescending(ms => ms.score)
                        .ToList();
                    Console.WriteLine($"Number of possible meetings positions: {meetingsToAdd.Count}");
                    Console.WriteLine($"Number of not placed meetings: {currentSchedule.NotUsedMeetings.Count}");
                    Console.WriteLine($"Number of placed meetings: {currentSchedule.Meetings.Count}");
                    Console.WriteLine();
                    if (meetingsToAdd.Count == 0)
                        break;
                    var bestMeeting = meetingsToAdd.First();
                    currentSchedule.AddMeeting(bestMeeting.Item1);
                }

                Console.WriteLine();
                Console.WriteLine(sw.Elapsed);
                Console.WriteLine();
                yield return new(currentSchedule, currentScore);
                yield break;
            }
        }

        private double EstimateResult(Schedule schedule, Meeting meeting)
        {
            schedule.AddMeeting(meeting);
            var score = estimator.Estimate(schedule);
            schedule.RemoveMeeting(meeting);
            return score;
        }
    }
}