using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Domain.Enums;
using Domain.MeetingsParts;

namespace Domain.Algorithms.Solvers
{
    public class GreedySolver : ISolver
    {
        private readonly IEstimator estimator;
        private readonly Requisition requisition;
        private readonly Dictionary<string, List<RoomSpec>> classroomsWithSpecs;
        private readonly int choiceCount;

        private readonly Random random;

        public GreedySolver(IEstimator estimator, Requisition requisition,
            Dictionary<string, List<RoomSpec>> classroomsWithSpecs, Random random, int choiceCount = 1)
        {
            this.estimator = estimator;
            this.requisition = requisition;
            this.classroomsWithSpecs = classroomsWithSpecs;
            this.random = random;
            this.choiceCount = choiceCount;
        }

        public Solution GetSolution(TimeSpan timeBudget)
        {
            var sw = Stopwatch.StartNew();
            var currentSchedule = new Schedule(requisition, classroomsWithSpecs);
            //while (sw.Elapsed < timeBudget)
            //empty schedule is scored too high
            while (true)
            {
                var meetingsToAdd = currentSchedule.GetMeetingsToAdd()
                    .Select(meetings => (meetings, score: EstimateResult(currentSchedule, meetings)))
                    .OrderByDescending(ms => ms.score)
                    .ToList();
                Console.WriteLine($"Possible meetings positions: {meetingsToAdd.Count}");
                Console.WriteLine($"Not placed meetings: {currentSchedule.NotUsedMeetings.Count}");
                Console.WriteLine($"Placed meetings: {currentSchedule.Meetings.Count}");
                Console.WriteLine();
                if (meetingsToAdd.Count == 0)
                {
                    var notUsedMeetings = string.Join("\n", currentSchedule.NotUsedMeetings);
                    Console.WriteLine(notUsedMeetings);

                    break;
                }

                var maxIndex = Math.Min(choiceCount, meetingsToAdd.Count);
                var (meeting, _) = meetingsToAdd[random.Next(maxIndex)];
                currentSchedule.AddMeeting(meeting, true);
            }

            sw.Stop();

            var currentScore = estimator.Estimate(currentSchedule);
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