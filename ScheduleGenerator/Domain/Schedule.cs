using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Conversions;
using Domain.Enums;
using Domain.MeetingsParts;
using static Domain.DomainExtensions;

namespace Domain
{
    public interface IReadonlySchedule
    {
        IReadOnlySet<Meeting> GetMeetings();
    }

    public class Schedule : IReadonlySchedule
    {
        public readonly HashSet<Meeting> Meetings = new();
        public readonly HashSet<Meeting> NotUsedMeetings;
        public readonly HashSet<Meeting> NonPlaceableMeetings = new();

        public readonly Requisition Requisition;
        public readonly Dictionary<string, List<RoomSpec>> SpecsByRoom;
        public readonly Dictionary<RoomSpec, HashSet<string>> RoomsBySpec = new();

        public readonly Dictionary<MeetingGroup, Dictionary<WeekType, Dictionary<DayOfWeek, Meeting?[]>>>
            GroupMeetingsByTime = new();

        public readonly Dictionary<Teacher, Dictionary<WeekType, Dictionary<DayOfWeek, Meeting?[]>>>
            TeacherMeetingsByTime = new();

        public readonly Dictionary<MeetingGroup, Dictionary<LearningPlanItem, double>> GroupLearningPlanItemsCount =
            new();

        public readonly Dictionary<MeetingTime, HashSet<string>> FreeRoomsByDay = new();

        public readonly Dictionary<MeetingTime, HashSet<Meeting>> MeetingsByTimeSlot = new();

        private readonly Dictionary<MeetingGroup, Dictionary<MeetingTime, Dictionary<WeekType, HashSet<Meeting>>>>
            timeConcurrentMeetings = new();

        public readonly Dictionary<Meeting, int> MeetingFreedomDegree = new();

        public readonly Dictionary<MeetingGroup,
            Dictionary<Discipline, Dictionary<MeetingType, Dictionary<Teacher, double>>>> GroupTeachersByDiscipline =
            new();

        public readonly HashSet<MeetingGroup> Groups;

        public Schedule(Requisition requisition, Dictionary<string, List<RoomSpec>> specsByRoom)
        {
            Groups = requisition.Items.SelectMany(DomainExtensions.GetAllGroupParts).ToHashSet();
            
            FillTeachersKeys(requisition);
            FillGroupsKeys();

            Requisition = requisition;
            SpecsByRoom = specsByRoom;
            FillClassroomsBySpec(specsByRoom);
            FillRoomPool(specsByRoom.Keys);
            NotUsedMeetings = requisition.Items
                .SelectMany(RequisitionToMeetingConverter.ConvertRequisitionToBaseMeeting)
                .ToHashSet();
            LinkBaseMeetings(NotUsedMeetings);
            FillTimeToMeetingsDictionaries(NotUsedMeetings);
        }

        public override bool Equals(object? obj)
        {
            return this.ToString().Equals(obj?.ToString());
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public override string ToString()
        {
            return string.Join("\n", Meetings.Select(m => m.ToString()).OrderBy(s => s));
        }

        public Schedule Copy()
        {
            // TODO: optimize
            var copy = new Schedule(Requisition, SpecsByRoom);
            foreach (var meeting in Meetings)
            {
                if (copy.Meetings.Contains(meeting)) continue;
                copy.AddMeeting(meeting, true);
            }

            return copy;
        }

        private void FillGroupsKeys()
        {
            foreach (var group in Groups)
            {
                GroupTeachersByDiscipline[group] = new();
                GroupMeetingsByTime[group] = new();
                GroupLearningPlanItemsCount[group] = new();
                timeConcurrentMeetings[group] = new();
            }
        }

        private void FillTeachersKeys(Requisition requisition)
        {
            var teachers = requisition.Items
                .Select(r => r.Teacher)
                .Distinct();

            foreach (var teacher in teachers) TeacherMeetingsByTime[teacher] = new();
        }

        private void FillTimeToMeetingsDictionaries(IEnumerable<Meeting> meetings)
        {
            foreach (var meeting in meetings)
            {
                var possibleTimeChoices = meeting.RequisitionItem.GetAllMeetingTimes();
                var groups = meeting.RequisitionItem.GetAllGroupParts();

                var weekTypes = meeting.WeekType.GetPossibleWeekTypes();

                foreach (var group in groups)
                foreach (var time in possibleTimeChoices)
                foreach (var weekType in weekTypes)
                    timeConcurrentMeetings.SafeAdd(group, time, weekType, meeting);

                var weekTypeDegree = meeting.WeekType == WeekType.OddOrEven ? 2 : 1;
                var groupDegree = meeting.RequisitionItem.GroupPriorities.Sum(g => g.GroupsChoices.Length);
                MeetingFreedomDegree.Add(meeting, possibleTimeChoices.Count * weekTypeDegree * groupDegree);

                foreach (var timeChoice in possibleTimeChoices) MeetingsByTimeSlot.SafeAdd(timeChoice, meeting);
            }
        }

        public IReadOnlySet<Meeting> GetMeetings()
        {
            return Meetings;
        }

        private void UnsubscribeCollidingMeetings(Meeting meeting)
        {
            var time = meeting.MeetingTime!;
            foreach (var group in meeting.GroupsChoice!.GetGroupParts())
            foreach (var weekType in meeting.WeekType.GetWeekTypes())
            {
                if (!timeConcurrentMeetings[group].TryGetValue(time, weekType, out var concurrentMeetings)) continue;
                foreach (var concurrentMeeting in concurrentMeetings.ToList())
                    UnsubscribeMeetingFromCell(concurrentMeeting, group, time, weekType);
            }
        }

        private void ResetMeetingsSubscriptions()
        {
            timeConcurrentMeetings.Clear();

            foreach (var baseMeeting in NotUsedMeetings)
            {
                MeetingFreedomDegree[baseMeeting] = GetFilledMeetings(baseMeeting)
                    .Count(SubscribeMeetingToCells);
            }
        }

        private bool SubscribeMeetingToCells(Meeting filledMeeting)
        {
            var baseMeeting = filledMeeting.BaseMeeting!;

            var weekTypes = filledMeeting.WeekType.GetWeekTypes();
            var time = filledMeeting.MeetingTime!;

            var addAny = false;

            foreach (var group in filledMeeting.GroupsChoice!.GetGroupParts())
            foreach (var week in weekTypes)
                addAny |= timeConcurrentMeetings.SafeAdd(group, time, week, baseMeeting);

            return addAny;
        }

        private void UnsubscribeMeetingFromCell(Meeting collidingMeeting, MeetingGroup meetingGroup, MeetingTime time,
            WeekType weekType)
        {
            WeekType[] weekTypes = collidingMeeting.WeekType == WeekType.All
                ? ArrayExtensions.OddAndEven
                : new[] {weekType};
            var groupsChoices = collidingMeeting.RequisitionItem.GroupPriorities
                .SelectMany(g => g.GroupsChoices)
                .Where(g => g.GetGroupParts().Contains(meetingGroup));

            foreach (var groupsChoice in groupsChoices)
            {
                var removedAny = false;

                foreach (var group in groupsChoice.GetGroupParts())
                foreach (var week in weekTypes)
                    removedAny |= timeConcurrentMeetings[group][time][week].Remove(collidingMeeting);

                if (removedAny) MeetingFreedomDegree[collidingMeeting] -= 1;
            }
        }

        public void AddMeeting(Meeting meeting, bool isSure = false)
        {
            foreach (var meetingToAdd in meeting.GetLinkedMeetings())
            {
                Meetings.Add(meetingToAdd);

                var teacher = meetingToAdd.Teacher;
                var meetingTime = meetingToAdd.MeetingTime!;
                TeacherMeetingsByTime.SafeAdd(teacher, meetingToAdd);

                if (meetingToAdd.Classroom != null)
                    FreeRoomsByDay[meetingTime].Remove(meetingToAdd.Classroom);
                AddMeetingToGroup(meetingToAdd);

                NotUsedMeetings.Remove(meetingToAdd.BaseMeeting!);

                if (isSure) UnsubscribeCollidingMeetings(meetingToAdd);
            }
        }

        public void RemoveMeeting(Meeting meeting, bool isSure = false)
        {
            foreach (var meetingToRemove in meeting.GetLinkedMeetings())
            {
                Meetings.Remove(meetingToRemove);

                var meetingTime = meetingToRemove.MeetingTime!;
                var (day, timeSlot) = meetingTime;
                foreach (var weekType in meetingToRemove.WeekType.GetWeekTypes())
                    TeacherMeetingsByTime[meetingToRemove.Teacher][weekType][day][timeSlot] = null;

                if (meetingToRemove.Classroom != null)
                    FreeRoomsByDay[meetingTime].Add(meetingToRemove.Classroom);

                RemoveMeetingFromGroup(meetingToRemove);

                NotUsedMeetings.Add(meetingToRemove.BaseMeeting!);

                if (isSure) ResetMeetingsSubscriptions();
            }
        }

        public IEnumerable<Meeting> GetMeetingsToAdd()
        {
            var placeableMeetings = NotUsedMeetings
                .Where(m => !NonPlaceableMeetings.Contains(m))
                .ToList();
            if (placeableMeetings.Count == 0)
            {
                return NotUsedMeetings.ToList()
                    .SelectMany(b => GetFilledMeetings(b, true));
            }

            var minFreedomMeetings = GetMostNeededMeetings(placeableMeetings);

            var meetingsCopies = new List<Meeting>();
            foreach (var baseMeeting in minFreedomMeetings)
            {
                var filledMeetings = GetFilledMeetings(baseMeeting).ToList();
                meetingsCopies.AddRange(filledMeetings);

                if (filledMeetings.Count == 0)
                    NonPlaceableMeetings.Add(baseMeeting);
            }

            if (meetingsCopies.Count != 0) return meetingsCopies;
            return GetMeetingsToAdd();
        }

        private List<Meeting> GetMostNeededMeetings(List<Meeting> meetings)
        {
            var priority = meetings.Max(m => m.Priority);
            var priorityMeetings = meetings
                .Where(m => m.Priority == priority)
                .ToList();

            var minFreedom = priorityMeetings.Min(m => MeetingFreedomDegree[m]);
            var minFreedomMeetings = priorityMeetings
                .Where(m => MeetingFreedomDegree[m] == minFreedom)
                .ToList();

            return minFreedomMeetings;
        }

        private IEnumerable<Meeting> GetFilledMeetings(Meeting baseMeeting, bool ignoreTimePriorities = false)
        {
            var requisitionItem = baseMeeting.RequisitionItem;
            var possibleGroupsChoices = requisitionItem.GroupPriorities
                .SelectMany(p => p.GroupsChoices);
            var possibleTimeChoices = ignoreTimePriorities
                ? GetAllPossibleMeetingTimes().ToHashSet()
                : requisitionItem.GetAllMeetingTimes();

            foreach (var groupsChoice in possibleGroupsChoices)
            foreach (var meetingTimeChoice in possibleTimeChoices)
            {
                var meetingCopy = TryCreateFilledMeeting(baseMeeting, groupsChoice, meetingTimeChoice,
                    ignoreTimePriorities);
                if (meetingCopy == null) continue;
                if (baseMeeting.RequiredAdjacentMeeting != null)
                {
                    if (meetingTimeChoice.TimeSlot < 2)
                        continue;
                    var linkedMeetingTimeChoice = new MeetingTime(meetingTimeChoice.Day,
                        meetingTimeChoice.TimeSlot - 1);
                    var linkedMeeting = TryCreateFilledMeeting(baseMeeting.RequiredAdjacentMeeting,
                        groupsChoice, linkedMeetingTimeChoice, ignoreTimePriorities);

                    if (linkedMeeting == null) continue;
                    meetingCopy.Link(linkedMeeting);
                }

                yield return meetingCopy;
            }
        }

        private static void LinkBaseMeetings(HashSet<Meeting> notUsedMeetings)
        {
            foreach (var meeting in notUsedMeetings)
            {
                var requiredAdjacentMeetingType = meeting.PlanItem.RequiredAdjacentMeetingType;
                if (requiredAdjacentMeetingType == null) continue;
                if (meeting.RequiredAdjacentMeeting != null) continue;
                var linkedMeeting = notUsedMeetings
                    .Where(m => m.RequiredAdjacentMeeting == null)
                    .Where(m => m.Teacher == meeting.Teacher)
                    .FirstOrDefault(e => e.Discipline.Equals(meeting.Discipline)
                                         && e.MeetingType.Equals(requiredAdjacentMeetingType)
                                         && !ReferenceEquals(e, meeting));
                if (linkedMeeting == null)
                    throw new ArgumentException(meeting.ToString());
                meeting.Link(linkedMeeting);
            }
        }

        private void FillClassroomsBySpec(Dictionary<string, List<RoomSpec>> classroomsWithSpecs)
        {
            foreach (var key in classroomsWithSpecs.Keys)
            foreach (var spec in classroomsWithSpecs[key])
                RoomsBySpec.SafeAdd(spec, key);
        }

        private void FillRoomPool(IReadOnlyCollection<string> rooms)
        {
            foreach (var time in GetAllPossibleMeetingTimes()) FreeRoomsByDay.Add(time, rooms.ToHashSet());
        }

        private Meeting? TryCreateFilledMeeting(Meeting baseMeeting, GroupsChoice groupsChoice, MeetingTime meetingTime,
            bool ignoreTimePriorities)
        {
            var meetingCopy = baseMeeting.BasicCopy();
            meetingCopy.GroupsChoice = groupsChoice;
            meetingCopy.MeetingTime = meetingTime;

            if (baseMeeting.IsRoomNeeded)
            {
                var room = FindFreeRoom(meetingTime, baseMeeting.PlanItem.RoomSpecs);
                if (room == null) return null;
                meetingCopy.Classroom = room;
            }

            WeekType[] weekTypes = baseMeeting.WeekType == WeekType.All
                ? ArrayExtensions.All
                : baseMeeting.WeekType.GetPossibleWeekTypes();

            foreach (var weekType in weekTypes)
            {
                meetingCopy.WeekType = weekType;

                if (IsMeetingValid(meetingCopy, ignoreTimePriorities)) return meetingCopy;
            }

            return null;
        }

        private bool IsMeetingValid(Meeting meeting, bool ignoreTimePriorities)
        {
            var timeAcceptableForTeacher = ignoreTimePriorities || IsTimeAcceptableForTeacher(meeting);
            return !(HasMeetingAlreadyAtThisTime(meeting) // weekType requires
                     || IsMeetingIsExtraForGroup(meeting)
                     || TeacherHasMeetingAlreadyAtThisTime(meeting) // weekType requires
                     || IsNoSpaceBetweenDifferentLocatedMeetings(meeting) // weekType requires
                     || !timeAcceptableForTeacher
                     || IsTeacherExtraForGroup(meeting)
                     || IsGroupExtraForTeacher(meeting)
                );
        }

        private string? FindFreeRoom(MeetingTime meetingTime, IEnumerable<RoomSpec> roomRequirement)
        {
            var possibleRooms = (IEnumerable<string>) FreeRoomsByDay[meetingTime];
            foreach (var rs in roomRequirement)
                possibleRooms = possibleRooms.Intersect(RoomsBySpec[rs]);
            return possibleRooms.OrderBy(e => SpecsByRoom[e].Count).FirstOrDefault();
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

        private bool IsMeetingIsExtraForGroup(Meeting meetingToAdd)
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

        private bool IsTeacherExtraForGroup(Meeting meetingToAdd)
        {
            var discipline = meetingToAdd.Discipline;

            foreach (var meetingGroup in meetingToAdd.GroupsChoice!.GetGroupParts())
            {
                if (!GroupTeachersByDiscipline.TryGetValue(meetingGroup, discipline,
                    out var byDiscipline)) continue;
                if (!byDiscipline.TryGetValue(meetingToAdd.MeetingType,
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

        private void AddMeetingToGroup(Meeting meetingToAdd)
        {
            var discipline = meetingToAdd.Discipline;
            var meetingType = meetingToAdd.MeetingType;
            var teacher = meetingToAdd.Teacher;
            var value = meetingToAdd.Weight;
            var planItem = meetingToAdd.PlanItem;
            foreach (var meetingGroup in meetingToAdd.GroupsChoice!.GetGroupParts())
            {
                GroupMeetingsByTime.SafeAdd(meetingGroup, meetingToAdd);
                GroupLearningPlanItemsCount.SafeIncrement(meetingGroup, planItem, value);

                var byDiscipline = GroupTeachersByDiscipline.SafeAddAndReturn(meetingGroup, discipline, new());

                byDiscipline.SafeIncrement(meetingType, teacher, value);
            }
        }

        private void RemoveMeetingFromGroup(Meeting meetingToRemove)
        {
            var (day, timeSlot) = meetingToRemove.MeetingTime!;
            var discipline = meetingToRemove.Discipline;
            var meetingType = meetingToRemove.MeetingType;
            var teacher = meetingToRemove.Teacher;
            var value = meetingToRemove.Weight;
            var planItem = meetingToRemove.PlanItem;
            foreach (var meetingGroup in meetingToRemove.GroupsChoice!.GetGroupParts())
            {
                foreach (var weekType in meetingToRemove.WeekType.GetWeekTypes())
                    GroupMeetingsByTime[meetingGroup][weekType][day][timeSlot] = null;

                GroupLearningPlanItemsCount.SafeIncrement(meetingGroup, planItem, -value);
                GroupTeachersByDiscipline[meetingGroup][discipline].SafeIncrement(meetingType, teacher, -value);
            }
        }
    }
}