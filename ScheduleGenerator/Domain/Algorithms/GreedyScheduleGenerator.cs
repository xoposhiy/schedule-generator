using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Domain.Rules;
using Domain.ScheduleLib;

namespace Domain.Algorithms
{
    public class GreedyScheduleGenerator : IScheduleGenerator
    {
        public Schedule MakeSchedule(LearningPlan learningPlan, MeetingEvaluator evaluator, Requisition requisition)
        {
            var sw = Stopwatch.StartNew();
            var meetingsToFill = requisition.Items.Select(
                Conversions.RequisitionToMeetingConverter.ConvertRequisitionToMeetingWithoutTime).SelectMany(x=>x).ToArray();
            var bestSchedule = new Schedule(new Meeting[0]);
            var bestPenalty = double.PositiveInfinity;
            for (var i = 0; i < 100; ++i)
            {
                var shuffledMeetingsToFill = meetingsToFill.Shuffled();
                var tryingResult = TryMakeSchedule(learningPlan, evaluator, shuffledMeetingsToFill, requisition);
                Console.WriteLine($"{tryingResult.Item1.Meetings.Length} {tryingResult.Item2} {bestPenalty > tryingResult.Item2}");
                if (bestPenalty > tryingResult.Item2)
                {
                    bestPenalty = tryingResult.Item2;
                    bestSchedule = tryingResult.Item1;
                }
            }

            Console.WriteLine($"Algo time: {sw.Elapsed}");
            Console.WriteLine($"Penalty: {bestPenalty}");
            return bestSchedule;
        }
        
        private (Schedule, double) TryMakeSchedule(LearningPlan learningPlan, MeetingEvaluator evaluator, Meeting[] meetingsToFill, Requisition requisition)
        {
            var penalty = 0d;
            var random = new Random();
            List<Meeting> currentMeetings = new();
            var allRequisitions = new List<RequisitionItem>();
            foreach (var requisitionItem in requisition.Items)
            {
                foreach (var groupPriority in requisitionItem.GroupPriorities)
                {
                    foreach (var groupsChoice in groupPriority.GroupsChoices)
                    {
                        var item = requisitionItem.Copy();
                        item.GroupPriorities = new GroupRequisition[] {new(new[] {groupsChoice})};
                        allRequisitions.Add(item);
                    }
                }
            }

            foreach (var meeting in meetingsToFill)
            {
                // var requisitionItem = GetCorrespondingRequisitionItem(meeting, requisition);
                var requisitionItem = GetCorrespondingRequisitionItem1(meeting, allRequisitions);
                var meetingsByPenalty = new SortedDictionary<double, List<(Meeting, RequisitionItem)>>();
                var schedule = new Schedule(currentMeetings.ToArray());

                foreach (var meetingTimePriority in requisitionItem.MeetingTimePriorities)
                {
                    foreach (var meetingTimeChoice in meetingTimePriority.MeetingTimeChoices)
                    {
                        var copy = meeting.Copy();
                        copy.MeetingTime = meetingTimeChoice;
                        copy.Groups = requisitionItem.GroupPriorities[0].GroupsChoices[0].Groups;
                        var currentPenalty = evaluator.Evaluate(learningPlan, requisition, schedule, copy);
                        if (meetingsByPenalty.ContainsKey(currentPenalty))
                            meetingsByPenalty[currentPenalty].Add((copy, requisitionItem));
                        else
                            meetingsByPenalty.Add(currentPenalty,
                                new List<(Meeting, RequisitionItem)> {(copy, requisitionItem)});
                    }
                }
                var bestMeetings = meetingsByPenalty.First().Value;
                var tuple = bestMeetings[random.Next(bestMeetings.Count)];
                var bestMeeting = tuple.Item1;
                allRequisitions.Remove(tuple.Item2);
                penalty += meetingsByPenalty.First().Key;
                currentMeetings.Add(bestMeeting);
                // Console.WriteLine($"{penalty}");
            }

            return (new Schedule(currentMeetings.ToArray()), penalty);
        }

        // private (Schedule, double) TryMakeSchedule(LearningPlan learningPlan, MeetingEvaluator evaluator, Meeting[] meetingsToFill, Requisition requisition)
        // {
        //     var penalty = 0d;
        //     var random = new Random();
        //     List<Meeting> currentMeetings = new();
        //     foreach(var meeting in meetingsToFill)
        //     {
        //         var requisitionItem = GetCorrespondingRequisitionItem(meeting, requisition);
        //         var meetingsByPenalty = new SortedDictionary<double, List<Meeting>>();
        //         var schedule = new Schedule(currentMeetings.ToArray());
        //
        //         foreach (var groupPriority in requisitionItem.GroupPriorities)
        //         {
        //             foreach (var groupsChoice in groupPriority.GroupsChoices)
        //             {
        //                 foreach (var meetingTimePriority in requisitionItem.MeetingTimePriorities)
        //                 {
        //                     foreach (var meetingTimeChoice in meetingTimePriority.MeetingTimeChoices)
        //                     {
        //                         var copy = meeting.Copy();
        //                         copy.MeetingTime = meetingTimeChoice;
        //                         copy.Groups = groupsChoice.Groups;
        //                         var currentPenalty = evaluator.Evaluate(learningPlan, requisition, schedule, copy);
        //                         if (meetingsByPenalty.ContainsKey(currentPenalty))
        //                             meetingsByPenalty[currentPenalty].Add(copy);
        //                         else
        //                             meetingsByPenalty.Add(currentPenalty, new List<Meeting>{copy});
        //                     }
        //                 }
        //             }
        //         }
        //
        //         var bestMeetings = meetingsByPenalty.First().Value;
        //         var bestMeeting = bestMeetings[random.Next(bestMeetings.Count)];
        //         penalty += meetingsByPenalty.First().Key;
        //         currentMeetings.Add(bestMeeting);
        //     }
        //
        //     return (new Schedule(currentMeetings.ToArray()), penalty);
        // }

        private RequisitionItem GetCorrespondingRequisitionItem(Meeting meeting, Requisition requisition)
        {
            foreach (var requisitionItem in requisition.Items)
            {
                if (requisitionItem.Teacher == meeting.Teacher
                    && requisitionItem.PlanItem.Discipline == meeting.Discipline
                    && requisitionItem.PlanItem.MeetingType == meeting.MeetingType)
                {
                    return requisitionItem;
                }
            }

            throw new Exception($"К встрече {meeting} не удалось подобрать соответствующее требование");
        }
        
        private RequisitionItem GetCorrespondingRequisitionItem1(Meeting meeting, List<RequisitionItem> requisitions)
        {
            foreach (var requisitionItem in requisitions)
            {
                if (requisitionItem.Teacher == meeting.Teacher
                    && requisitionItem.PlanItem.Discipline == meeting.Discipline
                    && requisitionItem.PlanItem.MeetingType == meeting.MeetingType)
                {
                    return requisitionItem;
                }
            }

            throw new Exception($"К встрече {meeting} не удалось подобрать соответствующее требование");
        }
    }
}
