using System;
using System.Collections.Generic;
using System.Linq;
using Domain.MeetingsParts;

namespace Domain.Algorithms.Solvers
{
    public class GreedySolver : ISolver
    {
        private readonly int choiceCount;
        private readonly IReadOnlyCollection<RoomRequisition> classroomsRequisitions;
        private readonly IEstimator estimator;
        private readonly Random random;
        private readonly Requisition requisition;
        private readonly bool selectWithBestScoreOnly;

        public GreedySolver(IEstimator estimator, Requisition requisition,
            IReadOnlyCollection<RoomRequisition> classroomsRequisitions,
            Random random, int choiceCount = 1,
            bool selectWithBestScoreOnly = true)
        {
            this.estimator = estimator;
            this.requisition = requisition;
            this.classroomsRequisitions = classroomsRequisitions;
            this.random = random;
            this.choiceCount = choiceCount;
            this.selectWithBestScoreOnly = selectWithBestScoreOnly;
        }

        public Solution GetSolution(TimeSpan timeBudget)
        {
            // var sw = Stopwatch.StartNew();
            var currentSchedule = new Schedule(requisition, classroomsRequisitions);
            return Solve(currentSchedule, timeBudget);
        }

        public Solution Solve(Schedule schedule, TimeSpan timeBudget)
        {
            while (true)
            {
                var meetingsToAdd = schedule.GetMeetingsToAdd()
                    .Select(meeting => (meeting, scoreDelta: EstimateResult(schedule, meeting)))
                    .OrderByDescending(ms => ms.scoreDelta)
                    .ToList();
                if (meetingsToAdd.Count == 0) break;

                var (meetingToAdd, _) = SelectNextMeeting(meetingsToAdd);
                //LoggerExtension.WriteLog(nextMeeting);
                schedule.AddMeeting(meetingToAdd, true);
                //WriteLog($"{estimator.Estimate(currentSchedule)}");
            }

            // WriteLog($"Not placed: {currentSchedule.NotUsedMeetings.Count}");
            // WriteLog($"Greedy {sw.Elapsed}\n");

            var score = estimator.Estimate(schedule);
            return new(schedule, score);
        }

        // ReSharper disable once UnusedTupleComponentInReturnValue
        private (Meeting meeting, double score) SelectNextMeeting(
            IReadOnlyList<(Meeting meeting, double score)> orderedMeetings)
        {
            if (choiceCount == 1) return orderedMeetings[0];

            var bestScore = orderedMeetings[0].score;
            var maxIndex = selectWithBestScoreOnly
                ? orderedMeetings.Count(m => Math.Abs(m.score - bestScore) < 0.001)
                : orderedMeetings.Count;
            maxIndex = Math.Min(maxIndex, choiceCount);

            return orderedMeetings[random.Next(maxIndex)];
        }

        private double EstimateResult(Schedule schedule, Meeting meeting)
        {
            // schedule.AddMeeting(meeting);
            // var score = estimator.Estimate(schedule);
            // schedule.RemoveMeeting(meeting);
            var score = estimator.EstimateMeetingToAdd(schedule, meeting);
            return score;
        }
    }
}