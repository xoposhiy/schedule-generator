using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Algorithms;
using Domain.Conversions;

namespace Domain.ScheduleLib
{
    public interface IReadonlySchedule
    {
        IEnumerable<Meeting> GetMeetings();
    }

    public class Schedule : IReadonlySchedule
    {
        public readonly Requisition Requisition;
        public readonly HashSet<Meeting> Meetings = new();
        public readonly HashSet<Meeting> NotUsedMeetings = new();
        public readonly Dictionary<string, List<RoomSpec>> SpecsByRoom;
        public readonly Dictionary<RoomSpec, List<string>> RoomsBySpec = new();
        public readonly Dictionary<MeetingGroup, Dictionary<MeetingTime, Meeting>> GroupMeetingsByTime = new();
        public readonly Dictionary<MeetingGroup, Dictionary<LearningPlanItem, int>> GroupLearningPlanItemsCount = new();
        public readonly Dictionary<Teacher, Dictionary<MeetingTime, Meeting>> TeacherMeetingsByTime = new();
        public readonly Dictionary<DayOfWeek, Dictionary<Teacher, SortedSet<int>>> TeacherMeetingsTimesByDay = new();

        public readonly Dictionary<DayOfWeek, Dictionary<MeetingGroup, SortedSet<int>>>
            GroupsMeetingsTimesByDay = new();

        public readonly Dictionary<MeetingTime, HashSet<string>> FreeRoomsByDay = new();
        public readonly Dictionary<Meeting, int> MeetingFreedomDegree = new();

        public Schedule(Meeting[] meetings)
        {
            Requisition = new Requisition(Array.Empty<RequisitionItem>());
            SpecsByRoom = new Dictionary<string, List<RoomSpec>>();
            Meetings = meetings.ToHashSet();
        }

        public Schedule(Requisition requisition, Dictionary<string, List<RoomSpec>> roomsWithSpecs)
        {
            Requisition = requisition;
            SpecsByRoom = roomsWithSpecs;
            FillClassroomsBySpec(roomsWithSpecs);
            FillRoomPool(roomsWithSpecs.Keys);
            NotUsedMeetings = requisition.Items
                .SelectMany(RequisitionToMeetingConverter.ConvertRequisitionToBasicMeeting)
                .ToHashSet();
            LinkBasicMeetings(NotUsedMeetings);
            FillMeetingFreedomDegree(NotUsedMeetings);
        }

        public IEnumerable<Meeting> GetMeetings()
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
                {
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
        }

        private void LinkBasicMeetings(HashSet<Meeting> notUsedMeetings)
        {
            foreach (var meeting in notUsedMeetings.ToList())
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
                {
                    foreach (var roomSpec in meeting.RequisitionItem.PlanItem.RoomSpecs)
                    {
                        possibleRooms.IntersectWith(RoomsBySpec[roomSpec]);
                    }
                }

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
            {
                foreach (var spec in classroomsWithSpecs[key])
                {
                    RoomsBySpec.SafeAdd(spec, key);
                }
            }
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
            var room = baseMeeting.RequisitionItem.IsOnline
                ? Meeting.OnlineLocationName
                : TryGetRoomFromPool(meetingTime, baseMeeting.RequisitionItem.PlanItem.RoomSpecs);
            if (room == null) return null;
            var meetingCopy = baseMeeting.BasicCopy();
            meetingCopy.Groups = groupsChoice.Groups;
            meetingCopy.MeetingTime = meetingTime;
            meetingCopy.Location = room;
            return !IsMeetingValid(groupsChoice, meetingTime, meetingCopy) ? null : meetingCopy;
        }

        //TODO сгруппировать методы в цикл
        private bool IsMeetingValid(GroupsChoice groupsChoice, MeetingTime meetingTime, Meeting meeting)
        {
            return !(IsCollisionMeetingToGroup(groupsChoice, meetingTime)
                     || IsOverfillMeetingToGroup(meeting, groupsChoice)
                     || IsCollisionMeetingToTeacher(meeting, meetingTime)
                     || IsNoGapBetweenOnlineOffline(groupsChoice, meetingTime, meeting)
                     || !DoesTimeSatisfyMeeting(meeting, meetingTime));
        }

        private string? TryGetRoomFromPool(MeetingTime meetingTime, RoomSpec[] roomRequirement)
        {
            var possibleRooms = FreeRoomsByDay[meetingTime].ToHashSet();
            foreach (var rs in roomRequirement)
                possibleRooms.IntersectWith(RoomsBySpec[rs]);
            return possibleRooms.OrderBy(e => SpecsByRoom[e].Count).FirstOrDefault();
        }

        private bool IsCollisionMeetingToTeacher(Meeting meetingToAdd, MeetingTime meetingTime)
        {
            var teacher = meetingToAdd.Teacher;
            if (TeacherMeetingsByTime.ContainsKey(teacher) &&
                TeacherMeetingsByTime[teacher].ContainsKey(meetingTime))
            {
                // Console.WriteLine($"Коллизия у препода {teacher} во время {meetingTimeChoice}, встреча {m}");
                return true;
            }

            return false;
        }

        private bool DoesTimeSatisfyMeeting(Meeting meeting, MeetingTime meetingTime)
        {
            return meeting.RequisitionItem.MeetingTimePriorities
                .Any(timePriority => timePriority.MeetingTimeChoices.Contains(meetingTime));
        }

        private bool CheckNeighbourMeeting(Meeting meeting, MeetingGroup group, MeetingTime meetingTime)
        {
            if (!GroupMeetingsByTime[group].TryGetValue(meetingTime, out var value)) return false;
            return value.RequisitionItem.IsOnline != meeting.RequisitionItem.IsOnline;
        }

        private bool IsNoGapBetweenOnlineOffline(GroupsChoice groupsChoice, MeetingTime meetingTime, Meeting meeting)
        {
            for (var dt = -1; dt < 2; dt += 2)
            {
                var time = meetingTime with {TimeSlotIndex = meetingTime.TimeSlotIndex + dt};
                foreach (var group in groupsChoice.GetGroupParts())
                {
                    if (!GroupMeetingsByTime.ContainsKey(group)) continue;
                    if (CheckNeighbourMeeting(meeting, group, time)) return true;
                }
            }

            return false;
        }

        private bool IsOverfillMeetingToGroup(Meeting meetingToAdd, GroupsChoice groupsChoice)
        {
            var planItem = meetingToAdd.RequisitionItem.PlanItem;
            foreach (var group in groupsChoice.GetGroupParts())
            {
                if (!GroupLearningPlanItemsCount.ContainsKey(group)) continue;
                if (CheckOverfillMeeting(group, planItem)) return true;
            }

            return false;
        }

        private bool CheckOverfillMeeting(MeetingGroup group, LearningPlanItem planItem)
        {
            return GroupLearningPlanItemsCount[group].ContainsKey(planItem)
                   && GroupLearningPlanItemsCount[group][planItem] ==
                   (int) Math.Ceiling(planItem.MeetingsPerWeek);
        }

        private bool IsCollisionMeetingToGroup(GroupsChoice groupsChoice, MeetingTime meetingTime)
        {
            foreach (var group in groupsChoice.GetGroupParts())
            {
                if (!GroupMeetingsByTime.ContainsKey(group)) continue;
                if (GroupMeetingsByTime[group].ContainsKey(meetingTime)) return true;
            }

            return false;
        }

        private List<Meeting> GetLinkedMeetings(Meeting meeting)
        {
            var meetings = new List<Meeting> {meeting};
            if (meeting.RequiredAdjacentMeeting != null)
                meetings.Add(meeting.RequiredAdjacentMeeting);
            return meetings;
        }


        private void AddMeetingToGroup(Meeting meetingToAdd, MeetingTime meetingTime)
        {
            foreach (var meetingGroup in meetingToAdd.Groups!.GetGroupParts())
            {
                var planItem = meetingToAdd.RequisitionItem.PlanItem;
                SafeAddMeetingToGroup(meetingToAdd, meetingTime, meetingGroup, planItem);
            }
        }

        private void SafeAddMeetingToGroup(Meeting meetingToAdd, MeetingTime meetingTime, MeetingGroup meetingGroup,
            LearningPlanItem planItem)
        {
            var (day, timeSlotIndex) = meetingTime;
            GroupMeetingsByTime.SafeAdd(meetingGroup, meetingTime, meetingToAdd);
            GroupLearningPlanItemsCount.SafeIncrement(meetingGroup, planItem);
            GroupsMeetingsTimesByDay.SafeAdd(day, meetingGroup, timeSlotIndex);
        }

        private void RemoveMeetingFromGroup(Meeting meetingToRemove, MeetingTime meetingTime)
        {
            foreach (var group in meetingToRemove.Groups!.GetGroupParts())
            {
                var planItem = meetingToRemove.RequisitionItem.PlanItem;
                SafeRemoveMeetingFromGroup(meetingTime, group, planItem);
            }
        }

        private void SafeRemoveMeetingFromGroup(MeetingTime meetingTime, MeetingGroup group, LearningPlanItem planItem)
        {
            var (day, timeSlotIndex) = meetingTime;
            GroupMeetingsByTime[group].Remove(meetingTime);
            GroupLearningPlanItemsCount.SafeDecrement(group, planItem);
            GroupsMeetingsTimesByDay[day][group].Remove(timeSlotIndex);
        }
    }
}