using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Domain.Enums;
using Domain.MeetingsParts;

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

        public Solution GetSolution(TimeSpan timeBudget)
        {
            var sw = Stopwatch.StartNew();
            var currentSchedule = new Schedule(requisition, classroomsWithSpecs);
            var currentScore = double.NegativeInfinity;
            while (sw.Elapsed < timeBudget)
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
                {
                    foreach (var notUsedMeeting in currentSchedule.NotUsedMeetings)
                    {
                        Console.WriteLine(notUsedMeeting);
                    }

                    break;
                }

                var (meeting, score) = meetingsToAdd.First();
                currentScore = score;
                currentSchedule.AddMeeting(meeting, true);
            }

            Console.WriteLine();
            Console.WriteLine($"Greedy {sw.Elapsed}");
            Console.WriteLine();
            return new(currentSchedule, currentScore);
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