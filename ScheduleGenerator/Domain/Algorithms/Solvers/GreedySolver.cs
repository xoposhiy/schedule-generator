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
        private readonly int choiceCount;
        private readonly Dictionary<string, List<RoomSpec>> classroomsWithSpecs;
        private readonly IEstimator estimator;
        private readonly Random random;
        private readonly Requisition requisition;
        private readonly bool selectWithBestScoreOnly;

        public GreedySolver(IEstimator estimator, Requisition requisition,
            Dictionary<string, List<RoomSpec>> classroomsWithSpecs, Random random, int choiceCount = 1,
            bool selectWithBestScoreOnly = true)
        {
            this.estimator = estimator;
            this.requisition = requisition;
            this.classroomsWithSpecs = classroomsWithSpecs;
            this.random = random;
            this.choiceCount = choiceCount;
            this.selectWithBestScoreOnly = selectWithBestScoreOnly;
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
                    .ToList();
                if (meetingsToAdd.Count == 0) break;

                var nextMeeting = SelectNextMeeting(meetingsToAdd);
                currentSchedule.AddMeeting(nextMeeting, true);
            }

            sw.Stop();

            Console.WriteLine($"Not placed: {currentSchedule.NotUsedMeetings.Count}");
            Console.WriteLine($"Greedy {sw.Elapsed}\n");

            var currentScore = estimator.Estimate(currentSchedule);
            return new Solution(currentSchedule, currentScore);
        }

        private Meeting SelectNextMeeting(IReadOnlyList<(Meeting meeting, double score)> orderedMeetings)
        {
            if (selectWithBestScoreOnly)
            {
                var bestScore = orderedMeetings[0].score;
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                var candidates = orderedMeetings.Where(m => m.score == bestScore).ToList();
                return candidates[random.Next(candidates.Count)].meeting;
            }
            else
            {
                var maxIndex = Math.Min(choiceCount, orderedMeetings.Count);
                return orderedMeetings[random.Next(maxIndex)].meeting;
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