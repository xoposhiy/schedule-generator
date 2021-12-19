using System.Collections.Generic;
using System.Linq;
using Domain.MeetingsParts;

namespace Domain
{
    public partial class Schedule
    {
        private bool IsMeetingValid(Meeting meeting, bool ignoreTimePriorities)
        {
            var timeAcceptableForTeacher = ignoreTimePriorities || IsTimeAcceptableForTeacher(meeting);
            return !(HasMeetingAlreadyAtThisTime(meeting) // weekType requires
                     || IsMeetingExtraForGroup(meeting) // weekType requires
                     || TeacherHasMeetingAlreadyAtThisTime(meeting) // weekType requires
                     || IsNoSpaceBetweenDifferentLocatedMeetings(meeting) // weekType requires
                     || IsUnwanted(meeting)
                     || !timeAcceptableForTeacher
                     || IsTeacherExtraForGroup(meeting)
                     || IsGroupExtraForTeacher(meeting)
                     || IsDayExtraForDiscipline(meeting)
                     || IsHardMeetingExtraForGroup(meeting)
                );
            //TODO: Обсудить оптимальный порядок условий
        }

        private bool TeacherHasMeetingAlreadyAtThisTime(Meeting meeting)
        {
            var teacher = meeting.Teacher;
            var timeSlot = meeting.MeetingTime!.TimeSlot;
            return TeacherMeetingsByTime.GetDaysByMeeting(teacher, meeting)
                .HasMeetingsAtTime(timeSlot);
        }

        private static bool IsTimeAcceptableForTeacher(Meeting meeting)
        {
            var meetingTime = meeting.MeetingTime!;
            return meeting.RequisitionItem.GetAllMeetingTimes().Contains(meetingTime);
        }

        private bool HasMeetingAlreadyAtThisTime(Meeting meeting)
        {
            var timeSlot = meeting.MeetingTime!.TimeSlot;
            return meeting.GroupsChoice!.GetGroupParts()
                .SelectMany(g => GroupMeetingsByTime.GetDaysByMeeting(g, meeting))
                .HasMeetingsAtTime(timeSlot);
        }

        private bool IsNoSpaceBetweenDifferentLocatedMeetings(Meeting meeting)
        {
            var timeSlotIndex = meeting.MeetingTime!.TimeSlot;
            var timeSlots = new[] {-1, 1}
                // var timeSlots = new[] {-2, -1, 1, 2}
                .Select(dt => timeSlotIndex + dt)
                .Where(ts => ts is > 0 and < 7)
                .ToList();
            var location = meeting.Location;
            foreach (var group in meeting.GroupsChoice!.GetGroupParts())
            foreach (var day in GroupMeetingsByTime.GetDaysByMeeting(group, meeting))
            foreach (var timeSlot in timeSlots)
            {
                if (day[timeSlot] == null) continue;
                if (day[timeSlot]!.Location != location) return true;
            }

            return false;
        }

        private bool IsMeetingExtraForGroup(Meeting meetingToAdd)
        {
            var planItem = meetingToAdd.PlanItem;
            var additionalWeight = meetingToAdd.Weight;
            foreach (var meetingGroup in meetingToAdd.GroupsChoice!.GetGroupParts())
            {
                if (!GroupLearningPlanItemsCount.TryGetValue(meetingGroup, planItem, out var weight)) continue;
                if (weight + additionalWeight > planItem.MeetingsPerWeek) return true;
            }

            return false;
        }

        private bool IsDayExtraForDiscipline(Meeting meetingToAdd)
        {
            var discipline = meetingToAdd.Discipline;
            foreach (var meetingGroup in meetingToAdd.GroupsChoice!.GetGroupParts())
            foreach (var weekType in meetingToAdd.WeekType.GetWeekTypes())
            {
                var days = 1;
                if (!GroupMeetingsByTime.TryGetValue(meetingGroup, weekType, out var byWeekType))
                    continue;

                foreach (var (dayOfWeek, day) in byWeekType)
                {
                    if (dayOfWeek == meetingToAdd.MeetingTime!.Day) continue;
                    if (day.Any(m => m?.Discipline == discipline))
                        days++;
                }

                if (days > 2)
                    return true;
            }

            return false;
        }

        private bool IsHardMeetingExtraForGroup(Meeting meetingToAdd)
        {
            if (!meetingToAdd.PlanItem.IsHard)
                return false;

            var dayOfWeek = meetingToAdd.MeetingTime!.Day;
            var discipline = meetingToAdd.Discipline;
            var meetingType = meetingToAdd.MeetingType;

            foreach (var meetingGroup in meetingToAdd.GroupsChoice!.GetGroupParts())
            foreach (var weekType in meetingToAdd.WeekType.GetWeekTypes())
            {
                if (!GroupMeetingsByTime.TryGetValue(meetingGroup, weekType, dayOfWeek, out var meetings))
                    continue;
                if (meetings.Any(m => m != null && m.Discipline == discipline && m.MeetingType == meetingType))
                    return true;
            }

            return false;
        }

        private bool IsTeacherExtraForGroup(Meeting meetingToAdd)
        {
            var discipline = meetingToAdd.Discipline;
            var meetingType = meetingToAdd.MeetingType;

            foreach (var meetingGroup in meetingToAdd.GroupsChoice!.GetGroupParts())
            {
                if (!GroupTeachersByDiscipline.TryGetValue(meetingGroup, discipline, meetingType,
                        out var byType)) continue;
                if (byType.Any(teacher => teacher.Value > 0
                                          && teacher.Key != meetingToAdd.Teacher))
                    return true;
            }

            return false;
        }

        private bool IsGroupExtraForTeacher(Meeting meeting)
        {
            var discipline = meeting.Discipline;
            var meetingType = meeting.MeetingType;
            var teacher = meeting.Teacher;
            var allGroups = meeting.RequisitionItem.GetAllGroupParts();
            var usedGroups = new HashSet<MeetingGroup>();
            foreach (var group in allGroups)
            {
                if (!GroupTeachersByDiscipline.TryGetValue(group, discipline, out var byDiscipline))
                    continue;
                if (!byDiscipline.TryGetValue(meetingType, teacher, out var meetingCount)) continue;
                if (meetingCount > 0)
                    usedGroups.Add(group);
            }

            usedGroups.UnionWith(meeting.GroupsChoice!.GetGroupParts());
            foreach (var groupRequisition in meeting.RequisitionItem.GroupPriorities)
            {
                var repetitionCount = 0;
                foreach (var groupsChoice in groupRequisition.GroupsChoices)
                    if (groupsChoice.GetGroupParts().IsSubsetOf(usedGroups))
                        repetitionCount++;

                if (repetitionCount > meeting.RequisitionItem.RepetitionsCount) return true;
            }

            return false;
        }

        private bool IsUnwanted(Meeting meeting)
        {
            var dayOfWeek = meeting.MeetingTime!.Day;
            foreach (var meetingGroup in meeting.GroupsChoice!.GetGroupParts())
            foreach (var weekType in meeting.WeekType.GetWeekTypes())
            {
                if (!GroupMeetingsByTime.TryGetValue(meetingGroup, weekType, dayOfWeek, out var meetings))
                    continue;

                if (meetings.Any(m => m != null && m.MeetingsHasConflict(meeting)))
                    return true;
            }

            return false;
        }
    }
}