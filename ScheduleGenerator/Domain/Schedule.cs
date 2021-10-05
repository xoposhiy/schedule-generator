using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Algorithms;
using Domain.Conversions;

namespace Domain
{
    public interface IReadonlySchedule
    {
        IReadOnlySet<Meeting> GetMeetings();
    }

    public class Schedule : IReadonlySchedule
    {
        public readonly HashSet<Meeting> Meetings = new();
        public readonly HashSet<Meeting> NotUsedMeetings = new();
        public readonly Dictionary<string, List<RoomSpec>> SpecsByRoom = new();
        public readonly Dictionary<RoomSpec, List<string>> RoomsBySpec = new();

        public readonly Dictionary<MeetingGroup, Dictionary<MeetingTime, Meeting>>
            GroupMeetingsByTime = new(); // TODO krutovsky: when see WeekType.All split to Odd and Even

        public readonly Dictionary<MeetingGroup, Dictionary<LearningPlanItem, int>> GroupLearningPlanItemsCount = new();
        public readonly Dictionary<Teacher, Dictionary<MeetingTime, Meeting>> TeacherMeetingsByTime = new();
        public readonly Dictionary<DayOfWeek, Dictionary<Teacher, SortedSet<int>>> TeacherMeetingsTimesByDay = new();

        public readonly Dictionary<DayOfWeek, Dictionary<MeetingGroup, SortedSet<int>>>
            GroupsMeetingsTimesByDay = new();

        public readonly Dictionary<MeetingTime, HashSet<string>> FreeRoomsByDay = new();
        public readonly Dictionary<Meeting, int> MeetingFreedomDegree = new();

        public Schedule(Meeting[] meetings)
        {
            Meetings = meetings.ToHashSet();
        }

        public Schedule(Requisition requisition, Dictionary<string, List<RoomSpec>> specsByRoom)
        {
            SpecsByRoom = specsByRoom;
            FillClassroomsBySpec(specsByRoom);
            FillRoomPool(specsByRoom.Keys);
            NotUsedMeetings = requisition.Items
                .SelectMany(RequisitionToMeetingConverter.ConvertRequisitionToBasicMeeting)
                .ToHashSet();
            LinkBasicMeetings(NotUsedMeetings);
            FillMeetingFreedomDegree(NotUsedMeetings);
        }

        public IReadOnlySet<Meeting> GetMeetings()
        {
            return Meetings;
        }

        public void AddMeeting(Meeting meeting)
        {
            var meetings = GetLinkedMeetings(meeting);

            foreach (var meetingToAdd in meetings)
            {
                Meetings.Add(meetingToAdd);

                var meetingTime = meetingToAdd.MeetingTime!;
                TeacherMeetingsByTime.SafeAdd(meetingToAdd.Teacher, meetingTime, meetingToAdd);
                FreeRoomsByDay[meetingTime].Remove(meetingToAdd.Location!);
                AddMeetingToGroup(meetingToAdd, meetingTime);

                TeacherMeetingsTimesByDay.SafeAdd(meetingTime.Day, meetingToAdd.Teacher, meetingTime.TimeSlotIndex);

                NotUsedMeetings.Remove(meetingToAdd.BaseMeeting!);
            }
        }

        public void RemoveMeeting(Meeting meeting)
        {
            var meetings = GetLinkedMeetings(meeting);

            foreach (var meetingToRemove in meetings)
            {
                Meetings.Remove(meetingToRemove);

                var meetingTime = meetingToRemove.MeetingTime!;
                TeacherMeetingsByTime[meetingToRemove.Teacher].Remove(meetingTime);
                if (meetingToRemove.Location != "Онлайн")
                    FreeRoomsByDay[meetingTime].Add(meetingToRemove.Location!);

                RemoveMeetingFromGroup(meetingToRemove, meetingTime);

                TeacherMeetingsTimesByDay[meetingTime.Day][meetingToRemove.Teacher].Remove(meetingTime.TimeSlotIndex);

                NotUsedMeetings.Add(meetingToRemove.BaseMeeting!);
            }
        }

        public IEnumerable<Meeting> GetMeetingsToAdd()
        {
            foreach (var meeting in NotUsedMeetings.ToList())
            {
                var requisitionItem = meeting.RequisitionItem;
                var possibleGroupsChoices = requisitionItem.GroupPriorities
                    .SelectMany(p => p.GroupsChoices);
                var possibleTimeChoices = requisitionItem.MeetingTimePriorities
                    .SelectMany(p => p.MeetingTimeChoices)
                    .ToHashSet();

                foreach (var groupsChoice in possibleGroupsChoices)
                foreach (var meetingTimeChoice in possibleTimeChoices)
                {
                    var meetingCopy = TryCreateFilledMeeting(meeting, groupsChoice, meetingTimeChoice);
                    if (meetingCopy == null) continue;
                    if (meetingCopy.RequiredAdjacentMeeting != null)
                    {
                        if (meetingTimeChoice.TimeSlotIndex < 2)
                            continue;
                        var linkedMeetingTimeChoice = new MeetingTime(meetingTimeChoice.Day,
                            meetingTimeChoice.TimeSlotIndex - 1, meetingCopy.WeekType);
                        var linkedMeeting = TryCreateFilledMeeting(meetingCopy.RequiredAdjacentMeeting,
                            groupsChoice,
                            linkedMeetingTimeChoice);

                        if (linkedMeeting == null) continue;
                        meetingCopy.RequiredAdjacentMeeting = linkedMeeting;
                        linkedMeeting.RequiredAdjacentMeeting = meetingCopy;
                    }

                    yield return meetingCopy;
                }
            }
        }

        private void LinkBasicMeetings(HashSet<Meeting> notUsedMeetings)
        {
            foreach (var meeting in notUsedMeetings)
            {
                var requiredAdjacentMeetingType = meeting.RequisitionItem.PlanItem.RequiredAdjacentMeetingType;
                if (requiredAdjacentMeetingType == null) continue;
                var linkedMeeting = notUsedMeetings
                    .FirstOrDefault(e => e.Discipline.Equals(meeting.Discipline)
                                         && e.Teacher.Equals(meeting.Teacher)
                                         && e.MeetingType.Equals(requiredAdjacentMeetingType)
                                         && !ReferenceEquals(e, meeting));
                if (linkedMeeting == null)
                    throw new FormatException(meeting.ToString());
                meeting.RequiredAdjacentMeeting = linkedMeeting;
                linkedMeeting.RequiredAdjacentMeeting = meeting;
            }
        }

        private void FillMeetingFreedomDegree(IEnumerable<Meeting> meetings)
        {
            foreach (var meeting in meetings)
            {
                var possibleRooms = SpecsByRoom.Keys.ToHashSet();
                if (!meeting.RequisitionItem.IsOnline)
                    foreach (var roomSpec in meeting.RequisitionItem.PlanItem.RoomSpecs)
                        possibleRooms.IntersectWith(RoomsBySpec[roomSpec]);

                var requisitionItem = meeting.RequisitionItem;
                var groupsChoicesCount = requisitionItem.GroupPriorities
                    .SelectMany(p => p.GroupsChoices)
                    .Count();
                var timeChoicesCount = requisitionItem.MeetingTimePriorities
                    .SelectMany(p => p.MeetingTimeChoices)
                    .Count();
                MeetingFreedomDegree.Add(meeting, groupsChoicesCount * timeChoicesCount * possibleRooms.Count);
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
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                var time = new MeetingTime(day, 0, WeekType.All);
                if (day == DayOfWeek.Sunday) continue;
                for (var i = 1; i < 7; i++)
                {
                    FreeRoomsByDay.Add(time with {TimeSlotIndex = i, WeekType = WeekType.Even}, rooms.ToHashSet());
                    FreeRoomsByDay.Add(time with {TimeSlotIndex = i, WeekType = WeekType.Odd}, rooms.ToHashSet());
                }
            }
        }

        private Meeting? TryCreateFilledMeeting(Meeting baseMeeting, GroupsChoice groupsChoice, MeetingTime meetingTime)
        {
            var room = baseMeeting.RequisitionItem.IsOnline
                ? Meeting.OnlineLocationName
                : FindFreeRoom(meetingTime, baseMeeting.RequisitionItem.PlanItem.RoomSpecs);
            if (room == null) return null;
            var meetingCopy = baseMeeting.BasicCopy();
            meetingCopy.Groups = groupsChoice.Groups;
            meetingCopy.MeetingTime = meetingTime;
            meetingCopy.Location = room;

            if (GetLinkedMeetings(meetingCopy).All(m => IsMeetingValid(groupsChoice, m)))
            {
                return meetingCopy;
            }

            return null;
        }

        private bool IsMeetingValid(GroupsChoice groupsChoice, Meeting meeting)
        {
            var meetingTime = meeting.MeetingTime!;
            return !(HasMeetingAlreadyAtThisTime(groupsChoice, meetingTime)
                     || IsMeetingIsExtraForGroup(meeting, groupsChoice)
                     || TeacherHasMeetingAlreadyAtThisTime(meeting, meetingTime)
                     || IsNoGapBetweenOnlineAndOfflineMeetings(groupsChoice, meetingTime, meeting)
                     || !IsTimeAcceptableForTeacher(meeting, meetingTime));
        }

        private string? FindFreeRoom(MeetingTime meetingTime, RoomSpec[] roomRequirement)
        {
            return FindFreeRooms(meetingTime, roomRequirement)
                .OrderBy(e => SpecsByRoom[e].Count).FirstOrDefault();
        }

        private HashSet<string> FindFreeRooms(MeetingTime meetingTime, RoomSpec[] roomRequirement)
        {
            if (meetingTime.WeekType == WeekType.All)
            {
                var odd = FindFreeRooms(meetingTime with {WeekType = WeekType.Odd}, roomRequirement);
                var even = FindFreeRooms(meetingTime with {WeekType = WeekType.Even}, roomRequirement);
                odd.IntersectWith(even);
                return odd;
            }

            var possibleRooms = FreeRoomsByDay[meetingTime].ToHashSet();
            foreach (var rs in roomRequirement)
                possibleRooms.IntersectWith(RoomsBySpec[rs]);
            return possibleRooms;
        }

        private bool TeacherHasMeetingAlreadyAtThisTime(Meeting meetingToAdd, MeetingTime meetingTime)
        {
            var teacher = meetingToAdd.Teacher;
            return TeacherMeetingsByTime.ContainsKey(teacher) &&
                   TeacherMeetingsByTime[teacher].ContainsKey(meetingTime);
        }

        private bool IsTimeAcceptableForTeacher(Meeting meeting, MeetingTime meetingTime)
        {
            return meeting.RequisitionItem.MeetingTimePriorities
                .Any(timePriority => timePriority.MeetingTimeChoices.Contains(meetingTime));
        }

        private bool HasMeetingAlreadyAtThisTime(GroupsChoice groupsChoice, MeetingTime meetingTime)
        {
            return groupsChoice.GetGroupParts()
                .Any(g => GroupMeetingsByTime.ContainsKey(g) && GroupMeetingsByTime[g].ContainsKey(meetingTime));
        }

        private bool HasMeetingAlready(MeetingGroup group, MeetingTime meetingTime, bool isOnline)
        {
            if (!GroupMeetingsByTime.ContainsKey(group)) return false;
            if (!GroupMeetingsByTime[group].TryGetValue(meetingTime, out var value)) return false;
            return value.RequisitionItem.IsOnline == isOnline;
        }

        private bool IsNoGapBetweenOnlineAndOfflineMeetings(GroupsChoice groupsChoice, MeetingTime meetingTime,
            Meeting meeting)
        {
            for (var dt = -1; dt < 2; dt += 2)
            {
                var time = meetingTime with {TimeSlotIndex = meetingTime.TimeSlotIndex + dt};
                if (groupsChoice.GetGroupParts()
                    .Any(g => HasMeetingAlready(g, time, !meeting.RequisitionItem.IsOnline)))
                    return true;
            }

            return false;
        }

        private bool IsMeetingIsExtraForGroup(Meeting meetingToAdd, GroupsChoice groupsChoice)
        {
            return groupsChoice.GetGroupParts()
                .Any(g => IsPlanItemFulfilled(g, meetingToAdd.RequisitionItem.PlanItem));
        }

        private bool IsPlanItemFulfilled(MeetingGroup group, LearningPlanItem planItem)
        {
            return GroupLearningPlanItemsCount.ContainsKey(group)
                   && GroupLearningPlanItemsCount[group].ContainsKey(planItem)
                   && GroupLearningPlanItemsCount[group][planItem] == (int) Math.Ceiling(planItem.MeetingsPerWeek);
            //TODO pe: это неверно в общем случае. Может быть поставлено три мигающих пары, что в сумме даст 1.5 пары в неделю.
        }

        private List<Meeting> GetLinkedMeetings(Meeting meeting)
        {
            if (meeting.MeetingTime!.WeekType != WeekType.All)
            {
                var meetings = new List<Meeting> {meeting};
                if (meeting.RequiredAdjacentMeeting != null)
                    meetings.Add(meeting.RequiredAdjacentMeeting);
                return meetings;
            }

            var oddMeeting = meeting.WithWeekType(WeekType.Odd);
            oddMeeting.RequiredAdjacentMeeting = oddMeeting.RequiredAdjacentMeeting?.WithWeekType(WeekType.Odd);
            var odd = GetLinkedMeetings(oddMeeting);

            var evenMeeting = meeting.WithWeekType(WeekType.Even);
            var even = GetLinkedMeetings(evenMeeting);
            
            odd.AddRange(even);

            return odd;
        }

        private void AddMeetingToGroup(Meeting meetingToAdd, MeetingTime meetingTime)
        {
            foreach (var meetingGroup in meetingToAdd.Groups!.GetGroupParts())
            {
                GroupMeetingsByTime.SafeAdd(meetingGroup, meetingTime, meetingToAdd);
                GroupLearningPlanItemsCount.SafeIncrement(meetingGroup, meetingToAdd.RequisitionItem.PlanItem);
                GroupsMeetingsTimesByDay.SafeAdd(meetingTime.Day, meetingGroup, meetingTime.TimeSlotIndex);
            }
        }

        private void RemoveMeetingFromGroup(Meeting meetingToRemove, MeetingTime meetingTime)
        {
            foreach (var meetingGroup in meetingToRemove.Groups!.GetGroupParts())
            {
                GroupMeetingsByTime[meetingGroup].Remove(meetingTime);
                GroupLearningPlanItemsCount.SafeDecrement(meetingGroup, meetingToRemove.RequisitionItem.PlanItem);
                GroupsMeetingsTimesByDay[meetingTime.Day][meetingGroup].Remove(meetingTime.TimeSlotIndex);
            }
        }
    }
}