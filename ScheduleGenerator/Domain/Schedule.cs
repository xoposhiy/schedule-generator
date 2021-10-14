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

        public readonly Dictionary<MeetingGroup, Dictionary<WeekType, Dictionary<DayOfWeek, Meeting?[]>>>
            GroupMeetingsByTime = new();

        public readonly Dictionary<Teacher, Dictionary<WeekType, Dictionary<DayOfWeek, Meeting?[]>>>
            TeacherMeetingsByTime = new(); //TODO: change to List(?)

        public readonly Dictionary<MeetingGroup, Dictionary<LearningPlanItem, int>> GroupLearningPlanItemsCount = new();

        public readonly Dictionary<MeetingTime, HashSet<string>> FreeRoomsByDay = new();
        public readonly Dictionary<Meeting, int> MeetingFreedomDegree = new();

        public readonly Dictionary<Meeting, HashSet<MeetingTime>> FreeTimeSlotByMeeting = new();
        public readonly Dictionary<MeetingTime, HashSet<Meeting>> MeetingsByTimeSlot = new();

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
            FillTimeToMeetingsDictionaries(NotUsedMeetings);

            FillMeetingFreedomDegree(NotUsedMeetings);
        }

        private void FillTimeToMeetingsDictionaries(IEnumerable<Meeting> meetings)
        {
            foreach (var meeting in meetings)
            {
                var requisitionItem = meeting.RequisitionItem;
                var possibleTimeChoices = requisitionItem.MeetingTimePriorities
                    .SelectMany(p => p.MeetingTimeChoices)
                    .ToHashSet();
                FreeTimeSlotByMeeting.Add(meeting, possibleTimeChoices);
                foreach (var timeChoice in possibleTimeChoices)
                {
                    if (!MeetingsByTimeSlot.ContainsKey(timeChoice))
                        MeetingsByTimeSlot[timeChoice] = new();
                    MeetingsByTimeSlot[timeChoice].Add(meeting);
                }
            }
        }

        public IReadOnlySet<Meeting> GetMeetings()
        {
            return Meetings;
        }

        public void AddMeeting(Meeting meeting, bool isSure = false)
        {
            foreach (var meetingToAdd in meeting.GetLinkedMeetings())
            {
                Meetings.Add(meetingToAdd);

                var teacher = meetingToAdd.Teacher;
                var meetingTime = meetingToAdd.MeetingTime!;
                TeacherMeetingsByTime.SafeAdd(teacher, meetingToAdd);

                FreeRoomsByDay[meetingTime].Remove(meetingToAdd.Location!);
                AddMeetingToGroup(meetingToAdd);

                NotUsedMeetings.Remove(meetingToAdd.BaseMeeting!);

                if (isSure)
                {
                    FreeTimeSlotByMeeting[meetingToAdd.BaseMeeting!].Remove(meetingTime);
                    MeetingsByTimeSlot[meetingTime].Remove(meetingToAdd.BaseMeeting!);
                }
            }

            if (isSure) FillMeetingFreedomDegree(NotUsedMeetings);
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

                if (meetingToRemove.Location != "Онлайн")
                    FreeRoomsByDay[meetingTime].Add(meetingToRemove.Location!);

                RemoveMeetingFromGroup(meetingToRemove);

                NotUsedMeetings.Add(meetingToRemove.BaseMeeting!);
                if (isSure)
                {
                    FreeTimeSlotByMeeting[meetingToRemove.BaseMeeting!].Add(meetingTime);
                    MeetingsByTimeSlot[meetingTime].Add(meetingToRemove.BaseMeeting!);
                }
            }

            if (isSure) FillMeetingFreedomDegree(NotUsedMeetings);
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
                            meetingTimeChoice.TimeSlotIndex - 1);
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
                var timeChoicesCount = FreeTimeSlotByMeeting[meeting].Count;
                //TODO: Optimize possibleRooms and groupsChoicesCount
                MeetingFreedomDegree[meeting] = groupsChoicesCount * timeChoicesCount * possibleRooms.Count;
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
                if (day == DayOfWeek.Sunday) continue;
                for (var i = 1; i < 7; i++)
                {
                    var time = new MeetingTime(day, i);
                    FreeRoomsByDay.Add(time, rooms.ToHashSet());
                }
            }
        }

        private Meeting? TryCreateFilledMeeting(Meeting baseMeeting, GroupsChoice groupsChoice, MeetingTime meetingTime)
        {
            WeekType[] weekTypes = baseMeeting.WeekType == WeekType.OddOrEven
                ? new[] {WeekType.Odd, WeekType.Even}
                : new[] {baseMeeting.WeekType};
            foreach (var weekType in weekTypes)
            {
                var meetingCopy = baseMeeting.BasicCopy();
                meetingCopy.WeekType = weekType;
                var room = baseMeeting.RequisitionItem.IsOnline
                    ? Meeting.OnlineLocationName
                    : FindFreeRoom(meetingTime, baseMeeting.RequisitionItem.PlanItem.RoomSpecs);
                if (room == null) return null;

                meetingCopy.Groups = groupsChoice.Groups;
                meetingCopy.MeetingTime = meetingTime;
                meetingCopy.Location = room;
                if (IsMeetingValid(meetingCopy)) return meetingCopy;
            }

            return null;
        }

        private bool IsMeetingValid(Meeting meeting)
        {
            return !(HasMeetingAlreadyAtThisTime(meeting)
                     || IsMeetingIsExtraForGroup(meeting)
                     || TeacherHasMeetingAlreadyAtThisTime(meeting)
                     || IsNoSpaceBetweenOnlineAndOfflineMeetings(meeting)
                     || !IsTimeAcceptableForTeacher(meeting));
        }

        private string? FindFreeRoom(MeetingTime meetingTime, IEnumerable<RoomSpec> roomRequirement)
        {
            var possibleRooms = FreeRoomsByDay[meetingTime].ToHashSet();
            foreach (var rs in roomRequirement)
                possibleRooms.IntersectWith(RoomsBySpec[rs]);
            return possibleRooms.OrderBy(e => SpecsByRoom[e].Count).FirstOrDefault();
        }

        private bool TeacherHasMeetingAlreadyAtThisTime(Meeting meeting)
        {
            var teacher = meeting.Teacher;
            var (day, timeSlotIndex) = meeting.MeetingTime!;
            if (!TeacherMeetingsByTime.TryGetValue(teacher, out var weekTypeByTeacher)) return false;
            foreach (var weekType in meeting.WeekType.GetWeekTypes())
            {
                if (!weekTypeByTeacher.TryGetValue(weekType, out var timeByWeekType)) continue;
                if (timeByWeekType.ContainsKey(day)
                    && timeByWeekType[day][timeSlotIndex] != null) return true;
            }

            return false;
        }

        private bool IsTimeAcceptableForTeacher(Meeting meeting)
        {
            var meetingTime = meeting.MeetingTime;
            return meeting.RequisitionItem.MeetingTimePriorities
                .Any(timePriority => timePriority.MeetingTimeChoices.Contains(meetingTime));
        }

        private bool HasMeetingAlreadyAtThisTime(Meeting meeting)
        {
            var (day, timeSlotIndex) = meeting.MeetingTime!;
            foreach (var group in meeting.Groups!.GetGroupParts())
            foreach (var weekType in meeting.WeekType.GetWeekTypes())
            {
                if (!GroupMeetingsByTime.TryGetValue(group, out var byGroup)) continue;
                if (!byGroup.TryGetValue(weekType, out var byWeekType)) continue;
                if (!byWeekType.TryGetValue(day, out var byDay)) continue;
                if (byDay[timeSlotIndex] != null) return true;
            }

            return false;
        }

        private bool HasMeetingAlready(MeetingGroup group, MeetingTime meetingTime, bool isOnline,
            WeekType meetingWeekType)
        {
            var (day, timeSlotIndex) = meetingTime;
            if (timeSlotIndex is -1 or 7) return false;
            if (!GroupMeetingsByTime.TryGetValue(group, out var byGroup)) return false;
            foreach (var weekType in meetingWeekType.GetWeekTypes())
            {
                if (!byGroup.TryGetValue(weekType, out var byWeekType)) continue;
                if (!byWeekType.TryGetValue(day, out var byDay)) continue;
                var value = byDay[timeSlotIndex];
                if (value == null) continue;
                if (value.RequisitionItem.IsOnline == isOnline)
                    return true;
            }

            return false;
        }

        private bool IsNoSpaceBetweenOnlineAndOfflineMeetings(Meeting meeting)
        {
            var meetingTime = meeting.MeetingTime!;
            for (var dt = -1; dt < 2; dt += 2)
            {
                var time = meetingTime with {TimeSlotIndex = meetingTime.TimeSlotIndex + dt};
                if (meeting.Groups!.GetGroupParts().Any(g => HasMeetingAlready(g, time,
                    !meeting.RequisitionItem.IsOnline, meeting.WeekType)))
                    return true;
            }

            return false;
        }

        private bool IsMeetingIsExtraForGroup(Meeting meetingToAdd)
        {
            return meetingToAdd.Groups!.GetGroupParts()
                .Any(g => IsPlanItemFulfilled(g, meetingToAdd.RequisitionItem.PlanItem));
        }

        private bool IsPlanItemFulfilled(MeetingGroup group, LearningPlanItem planItem)
        {
            return GroupLearningPlanItemsCount.ContainsKey(group)
                   && GroupLearningPlanItemsCount[group].ContainsKey(planItem)
                   && GroupLearningPlanItemsCount[group][planItem] == (int) Math.Ceiling(planItem.MeetingsPerWeek);
            //TODO pe: это неверно в общем случае. Может быть поставлено три мигающих пары, что в сумме даст 1.5 пары в неделю.
        }

        private void AddMeetingToGroup(Meeting meetingToAdd)
        {
            foreach (var meetingGroup in meetingToAdd.Groups!.GetGroupParts())
            {
                GroupMeetingsByTime.SafeAdd(meetingGroup, meetingToAdd);
                GroupLearningPlanItemsCount.SafeIncrement(meetingGroup, meetingToAdd.RequisitionItem.PlanItem);
            }
        }

        private void RemoveMeetingFromGroup(Meeting meetingToRemove)
        {
            var (day, timeSlotIndex) = meetingToRemove.MeetingTime!;
            foreach (var meetingGroup in meetingToRemove.Groups!.GetGroupParts())
            {
                foreach (var weekType in meetingToRemove.WeekType.GetWeekTypes())
                    GroupMeetingsByTime[meetingGroup][weekType][day][timeSlotIndex] = null;

                GroupLearningPlanItemsCount.SafeDecrement(meetingGroup, meetingToRemove.RequisitionItem.PlanItem);
            }
        }
    }
}