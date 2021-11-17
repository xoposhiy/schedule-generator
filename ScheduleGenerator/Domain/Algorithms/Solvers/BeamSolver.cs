using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Domain.Enums;
using Domain.MeetingsParts;
using static Infrastructure.LoggerExtension;

namespace Domain.Algorithms.Solvers
{
    public class BeamSolver : ISolver
    {
        private readonly int beamWidth;
        private readonly Dictionary<string, List<RoomSpec>> classroomsWithSpecs;

        private readonly IEstimator estimator;

        //private readonly Random random;
        private readonly Requisition requisition;

        public BeamSolver(IEstimator estimator, Requisition requisition,
            Dictionary<string, List<RoomSpec>> classroomsWithSpecs, /*Random random,*/ int beamWidth = 1)
        {
            this.estimator = estimator;
            this.requisition = requisition;
            this.classroomsWithSpecs = classroomsWithSpecs;
            //this.random = random;
            this.beamWidth = beamWidth;
        }

        public Solution GetSolution(TimeSpan timeBudget)
        {
            var sw = Stopwatch.StartNew();
            var currentSchedules = new List<Solution> {new(new(requisition, classroomsWithSpecs), 0d)};
            var totalCopiesCount = 0;
            var iterationCount = 0;
            
            while (true)
            {
                iterationCount++;
                var bestMeetings = GetBestMeetings(currentSchedules);
                var iteratedSolutions = GetIteratedSolutions(bestMeetings, out var copyCount);
                totalCopiesCount += copyCount;

                // WriteLog(iteratedSolutions.Count);
                if (iteratedSolutions.Count == 0)
                    break;

                currentSchedules = iteratedSolutions;
            }

            var bestSolution = currentSchedules.OrderByDescending(s => s.Score).First();
            WriteLog($"Beam width: {beamWidth}");
            WriteLog($"Mean copy count: {(double) totalCopiesCount / iterationCount}");
            WriteLog($"Elapsed: {sw.Elapsed}");
            WriteLog($"Best score: {bestSolution.Score}");
            return bestSolution;
        }

        private static List<Solution> GetIteratedSolutions(
            Dictionary<Schedule, List<(Schedule, Meeting, double score)>> bestMeetings, out int copyCount)
        {
            copyCount = 0;
            var newSchedules = new List<Solution>();
            foreach (var (schedule, variants) in bestMeetings)
            {
                for (var i = 0; i < variants.Count - 1; i++)
                {
                    copyCount++;
                    var copy = schedule.Copy();
                    copy.AddMeeting(variants[i].Item2, true);
                    newSchedules.Add(new(copy, variants[i].score));
                }

                schedule.AddMeeting(variants[^1].Item2, true);
                newSchedules.Add(new(schedule, variants[^1].score));
            }

            return newSchedules;
        }

        private Dictionary<Schedule, List<(Schedule, Meeting, double score)>> GetBestMeetings(
            List<Solution> currentSchedules)
        {
            var meetingsToAdd = new List<(Schedule, Meeting, double score)>();
            foreach (var (schedule, score) in currentSchedules)
                meetingsToAdd.AddRange(schedule.GetMeetingsToAdd()
                    .Select(meeting => (schedule, meeting, score: EstimateResult(schedule, meeting, score))));

            return meetingsToAdd
                .OrderByDescending(t => t.score)
                .Take(beamWidth)
                .GroupBy(t => t.Item1)
                .ToDictionary(g => g.Key, g => g.ToList());
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