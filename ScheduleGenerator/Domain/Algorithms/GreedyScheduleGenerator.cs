using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using Domain.Rules;
using Domain.ScheduleLib;

namespace Domain.Algorithms
{
    public class GreedyScheduleGenerator : IScheduleGenerator
    {
        public Schedule MakeSchedule(LearningPlan learningPlan, MeetingEvaluator evaluator, Requisition[] requisition)
        {
            var meetingsToFill = requisition.Select(
                Conversions.RequistionToMeetingConverter.ConvertRequistionToMeetingWithoutTime).SelectMany(x=>x).ToArray();
            var bestSchedule = new Schedule(new Meeting[0]);
            var bestPenalty = double.PositiveInfinity;
            for (var i = 0; i < 100; ++i)
            {
                var shuffledMeetingsToFill = meetingsToFill.Shuffled();
                var tryingResult = TryMakeSchedule(learningPlan, evaluator, shuffledMeetingsToFill, requisition);
                if (bestPenalty > tryingResult.Item2)
                {
                    bestPenalty = tryingResult.Item2;
                    bestSchedule = tryingResult.Item1;
                }
            }

            return bestSchedule;
        }

        private (Schedule, double) TryMakeSchedule(LearningPlan learningPlan, MeetingEvaluator evaluator, Meeting[] meetingsToFill, Requisition[] requisition)
        {
            var penalty = 0d;
            List<Meeting> currentMeetings = new List<Meeting>();
            foreach(var meeting in meetingsToFill)
            {
                var requisitionItem = GetCorrespondingRequisitionItem(meeting, requisition);
                var bestPenalty = double.PositiveInfinity;
                var bestMeeting = default(Meeting);

                foreach (var groupPriority in requisitionItem.GroupPriorities)
                {
                    foreach (var groupsChoice in groupPriority.GroupsChoices)
                    {
                        foreach (var meetingTimePriority in requisitionItem.MeetingTimePriorities)
                        {
                            foreach (var meetingTimeChoice in meetingTimePriority.MeetingTimeChoices)
                            {
                                meeting.MeetingTime = meetingTimeChoice;
                                meeting.Groups = groupsChoice.Groups;
                                var currentPenalty = evaluator.Evaluate(learningPlan, requisition, new Schedule(currentMeetings.ToArray()), meeting);
                                if (bestPenalty > currentPenalty)
                                {
                                    bestPenalty = currentPenalty;
                                    bestMeeting = meeting.Copy();
                                }
                            }
                        }
                    }
                }
                currentMeetings.Add(bestMeeting);
            }

            return (new Schedule(currentMeetings.ToArray()), penalty);
        }

        private Requisition GetCorrespondingRequisitionItem(Meeting meeting, Requisition[] requisition)
        {
            foreach (var requisitionItem in requisition)
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
