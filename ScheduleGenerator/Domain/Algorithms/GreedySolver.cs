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
        private readonly Random random;

        public GreedySolver(IEstimator estimator, Requisition requisition, Dictionary<string, List<RoomSpec>> classroomsWithSpecs, Random random)
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
                    if (meetingsToAdd.Count == 0)
                        break;
                    var bestMeeting = meetingsToAdd.First();
                    currentScore = bestMeeting.score;
                    foreach (var meeting in bestMeeting.meetings)
                    {
                        currentSchedule.AddMeeting(meeting);
                    }
                }
                yield return new Solution(currentSchedule, currentScore);
                yield break;
            }
        }

        private double EstimateResult(Schedule schedule, List<Meeting> meetings)
        {
            //return estimator.Estimate(scheduler.CurrentSolution.Schedule, meeting);
            foreach (var meeting in meetings)
                schedule.AddMeeting(meeting);
            var score = estimator.Estimate(schedule);
            foreach (var meeting in meetings)
                schedule.RemoveMeeting(meeting);
            return score;
        }
    }
}