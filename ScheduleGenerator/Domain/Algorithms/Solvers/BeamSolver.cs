using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Enums;
using Domain.MeetingsParts;
using Google.Apis.Sheets.v4.Data;

namespace Domain.Algorithms.Solvers
{
    public class BeamSolver : ISolver
    {
        private readonly int choiceCount;
        private readonly Dictionary<string, List<RoomSpec>> classroomsWithSpecs;
        private readonly IEstimator estimator;
        //private readonly Random random;
        private readonly Requisition requisition;
        //private readonly bool selectWithBestScoreOnly;

        public BeamSolver(IEstimator estimator, Requisition requisition,
            Dictionary<string, List<RoomSpec>> classroomsWithSpecs, Random random, int choiceCount = 1,
            bool selectWithBestScoreOnly = false)
        {
            this.estimator = estimator;
            this.requisition = requisition;
            this.classroomsWithSpecs = classroomsWithSpecs;
            //this.random = random;
            this.choiceCount = choiceCount;
            //this.selectWithBestScoreOnly = selectWithBestScoreOnly;
        }

        public Solution GetSolution(TimeSpan timeBudget)
        {
            var currentSchedules = new List<Solution> {new (new (requisition, classroomsWithSpecs), 0d)};
            var copyCount = 0;
            var iterationCount = 0;
            while (true)
            {
                iterationCount++;
                var meetings = new List<(Schedule, Meeting, double score)>();
                foreach (var (schedule, score) in currentSchedules)
                {
                    meetings.AddRange(schedule.GetMeetingsToAdd()
                        .Select(meeting => (schedule, meeting, score: EstimateResult(schedule, meeting, score))));
                }

                var newAllVariants = meetings
                    .OrderByDescending(t => t.score)
                    .Take(choiceCount)
                    .GroupBy(t => t.Item1)
                    .ToList();
                var newSchedules = new List<Solution>();
                foreach (var scheduleGroup in newAllVariants)
                {
                    var variants = scheduleGroup.ToList();
                    for (var i = 0; i < variants.Count - 1; i++)
                    {
                        copyCount++;
                        var copy = scheduleGroup.Key.Copy();
                        copy.AddMeeting(variants[i].Item2, true);
                        newSchedules.Add(new(copy, variants[i].score));
                    }

                    scheduleGroup.Key.AddMeeting(variants[^1].Item2, true);
                    newSchedules.Add(new(scheduleGroup.Key, variants[^1].score));
                }

                Console.WriteLine(newSchedules.Count);
                if (newSchedules.Count == 0)
                    break;
                
                currentSchedules = newSchedules;
            }
            Console.WriteLine((double)copyCount / iterationCount);
            
            return currentSchedules.OrderByDescending(s => s.Score).First();
        }

        private double EstimateResult(Schedule schedule, Meeting meeting, double baseScore)
        {
            // schedule.AddMeeting(meeting);
            // var score = estimator.Estimate(schedule);
            // schedule.RemoveMeeting(meeting);
            var scoreDelta = estimator.EstimateMeetingToAdd(schedule, meeting);
            return baseScore + scoreDelta;
        }
    }
}