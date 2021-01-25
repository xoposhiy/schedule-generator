using System.Collections.Generic;
using System.Linq;
using Domain.ScheduleLib;

namespace Domain.Rules
{
    class LecturerHasPracticeWithSameFlow : IRule
    {
        public readonly double UnitPenalty;

        public LecturerHasPracticeWithSameFlow(double unitPenalty = 1500)
        {
            UnitPenalty = unitPenalty;
        }

        public double Evaluate(LearningPlan learningPlan, Requisition[] requisition, Schedule schedule, Meeting meetingToAdd)
        {
            var hasPracticeWithSameFlow = CheckTeacherHasPracticeWithSameFlow(schedule, meetingToAdd, learningPlan);

            return hasPracticeWithSameFlow ? UnitPenalty : 0;
        }

        public static bool CheckTeacherHasPracticeWithSameFlow(Schedule schedule, Meeting meetingToAdd, LearningPlan learningPlan)
        {
            var planItemsWithSameDiscipline = learningPlan.Items.Where(m => m.Discipline == meetingToAdd.Discipline);
            var planedLectures = planItemsWithSameDiscipline.Where(m => m.MeetingType == MeetingType.Lecture);
            var planedPractices = planItemsWithSameDiscipline
                .Where(m => m.MeetingType == MeetingType.ComputerLab || m.MeetingType == MeetingType.Seminar);

            if (!planedLectures.Any() || !planedPractices.Any())
                return true;

            var meetingsWithSameDiscipline = schedule.Meetings.Where(m => m.Discipline == meetingToAdd.Discipline);
            var meetingsWithSameTeacherAndDiscipline = meetingsWithSameDiscipline.Where(m => m.Teacher == meetingToAdd.Teacher);

            var lectures = meetingsWithSameTeacherAndDiscipline
                .Where(m => m.MeetingType == MeetingType.Lecture)
                .ToHashSet();
            var practices = meetingsWithSameTeacherAndDiscipline
                .Where(m => m.MeetingType == MeetingType.ComputerLab || m.MeetingType == MeetingType.Seminar)
                .ToHashSet();

            if (meetingToAdd.MeetingType == MeetingType.Lecture)
            {
                lectures.Add(meetingToAdd);
            }
            else
            {
                practices.Add(meetingToAdd);
            }

            var lectureGroups = new HashSet<string>();
            var practiceGroups = new HashSet<string>();

            foreach (var lecture in lectures)
            {
                // Should be exactly one group specified by meeting
                foreach (var meetingGroup in lecture.Groups)
                {
                    lectureGroups.Add(meetingGroup.GroupName);
                }
            }

            foreach (var practice in practices)
            {
                // Should be exactly one group specified by meeting
                foreach (var meetingGroup in practice.Groups)
                {
                    practiceGroups.Add(meetingGroup.GroupName);
                }
            }

            var intersection = lectureGroups.Intersect(practiceGroups);
            var hasPracticeWithSameFlow = intersection.Any();
            return hasPracticeWithSameFlow;
        }
    }
}
