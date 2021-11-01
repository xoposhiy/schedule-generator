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
        private readonly Random random;
        private readonly int choiceCount;

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
            while (true)
            {
                var meetingsToAdd = currentSchedule.GetMeetingsToAdd()
                    .Select(meeting => (meeting, score: EstimateResult(currentSchedule, meeting)))
                    .OrderByDescending(ms => ms.score)
                    .Select(ms => ms.meeting)
                    .ToList();
                // Console.WriteLine($"Possible meetings positions: {meetingsToAdd.Count}");
                // Console.WriteLine($"Not placed meetings: {currentSchedule.NotUsedMeetings.Count}");
                // Console.WriteLine($"Placed meetings: {currentSchedule.Meetings.Count}");
                // Console.WriteLine();
                if (meetingsToAdd.Count == 0)
                {
                    break;
                }

                var maxIndex = Math.Min(choiceCount, meetingsToAdd.Count);
                var meetingToAdd = meetingsToAdd[random.Next(maxIndex)];
                currentSchedule.AddMeeting(meetingToAdd, true);
            }

            sw.Stop();

            Console.WriteLine($"Not placed: {currentSchedule.NotUsedMeetings.Count}\n");
            Console.WriteLine($"Greedy {sw.Elapsed}\n");

            var currentScore = estimator.Estimate(currentSchedule);
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