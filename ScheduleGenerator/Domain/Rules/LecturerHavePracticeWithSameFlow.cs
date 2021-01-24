using System;
using Domain.ScheduleLib;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Rules
{
    class LecturerHavePracticeWithSameFlow
    {
        public readonly double UnitPenalty;

        public LecturerHavePracticeWithSameFlow(double unitPenalty = 1500)
        {
            UnitPenalty = unitPenalty;
        }

        public EvaluationResult Evaluate(Schedule schedule, Requisition requisition)
        {
            var badMeetings = GetBadMeetings(schedule);
            return new EvaluationResult
            (
                badMeetings.Length * UnitPenalty,
                badMeetings,
                "Преподавателю желательно вести практику у своего потока"
            );
        }

        private static Meeting[] GetBadMeetings(Schedule schedule)
        {
            var badMeetings = new List<Meeting>();
            var teacherLectureGroups = new Dictionary<string, HashSet<string>>();
            var teacherPracticeGroups = new Dictionary<string, HashSet<string>>();
            var teacherPracticeMeetings = new Dictionary<string, HashSet<Meeting>>();
            foreach (var teacherGrouping in schedule.Meetings.GroupBy(meeting => meeting.Teacher.Name))
            {
                foreach (var disciplineGrouping in teacherGrouping.GroupBy(m => m.Discipline.Name))
                {
                    var lectures = disciplineGrouping.Where(m => m.MeetingType == MeetingType.Lecture);
                    var practices = disciplineGrouping.Where(m => m.MeetingType == MeetingType.Seminar || m.MeetingType == MeetingType.ComputerLab);

                    teacherLectureGroups[teacherGrouping.Key] = new HashSet<string>();
                    teacherPracticeGroups[teacherGrouping.Key] = new HashSet<string>();
                    teacherPracticeMeetings[teacherGrouping.Key] = new HashSet<Meeting>();

                    foreach (var lecture in lectures)
                    {
                        // Should be exactly one group specified by meeting
                        foreach (var meetingGroup in lecture.Groups)
                        {
                            teacherLectureGroups[teacherGrouping.Key].Add(meetingGroup.GroupName);
                        }
                    }

                    // may be no practice
                    foreach (var practice in practices)
                    {
                        // Should be exactly one group specified by meeting
                        foreach (var meetingGroup in practice.Groups)
                        {
                            teacherPracticeGroups[teacherGrouping.Key].Add(meetingGroup.GroupName);
                        }
                        teacherPracticeMeetings[teacherGrouping.Key].Add(practice);
                    }
                }
            }

            foreach (var teacher in teacherLectureGroups.Keys)
            {
                var lectureGroups = teacherLectureGroups[teacher];
                var practiceGroups = teacherPracticeGroups[teacher];
                if (!practiceGroups.Any())
                {
                    continue;
                }

                var intersection = lectureGroups.Intersect(practiceGroups);
                if (!intersection.Any())
                {
                    badMeetings.Add(teacherPracticeMeetings[teacher].First());
                }
            }
            return badMeetings.ToArray();
        }
    }
}
