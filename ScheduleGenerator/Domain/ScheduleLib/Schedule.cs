using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Domain.Algorithms;
using Domain.Conversions;
using Google.Apis.Auth.OAuth2;

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
        public readonly Dictionary<string, Dictionary<GroupPart, Dictionary<DayOfWeek, Dictionary<int, Meeting>>>> GroupMeetingsByTime = new();
        public readonly Dictionary<string, Dictionary<GroupPart, Dictionary<LearningPlanItem, int>>> GroupLearningPlanItemsCount = new();
        public readonly Dictionary<Teacher, Dictionary<MeetingTime, Meeting>> TeacherMeetingsByTime = new();
        public readonly Dictionary<DayOfWeek, Dictionary<Teacher, SortedSet<int>>> TeacherMeetingsTimesByDay = new();
        public readonly Dictionary<DayOfWeek, Dictionary<string, Dictionary<GroupPart, SortedSet<int>>>> GroupsMeetingsTimesByDay = new();
        public readonly Dictionary<DayOfWeek, Dictionary<int, HashSet<string>>> FreeRoomsByDay = new();
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
                    throw new FormatException("BIBA");
                meeting.RequiredAdjacentMeeting = linkedMeeting;
                linkedMeeting.RequiredAdjacentMeeting = meeting;
            }
        }

        private void FillMeetingFreedomDegree(IEnumerable<Meeting> meetings)
        {
            foreach (var meeting in meetings)
            {
                var possibleRooms = SpecsByRoom.Keys.ToHashSet();
                foreach (var roomSpec in meeting.RequisitionItem.PlanItem.RoomSpecs
                    .Where(rs => rs != RoomSpec.Online))
                {
                    possibleRooms.IntersectWith(RoomsBySpec[roomSpec]);
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
                var roomsByTimeSlot = new Dictionary<int, HashSet<string>>();
                for (var i = 1; i < 7; i++)
                    roomsByTimeSlot.Add(i, rooms.ToHashSet());
                FreeRoomsByDay.Add(day, roomsByTimeSlot);
            }
        }

        public IEnumerable<Meeting> GetMeetingsToAdd()
        {
            foreach (var meeting in NotUsedMeetings.ToList())
            {
                var requisitionItem = meeting.RequisitionItem;
                var possibleGroupsChoices = requisitionItem.GroupPriorities
                    .SelectMany(p => p.GroupsChoices)
                    .ToHashSet();
                var possibleTimeChoices = requisitionItem.MeetingTimePriorities
                    .SelectMany(p => p.MeetingTimeChoices)
                    .ToHashSet();

                foreach (var groupsChoice in possibleGroupsChoices)
                {
                    foreach (var meetingTimeChoice in possibleTimeChoices)
                    {
                        var meetingCopy = SetMeetingInstance(meeting, groupsChoice, meetingTimeChoice);
                        if (meetingCopy == null) continue;
                        if (meetingCopy.RequiredAdjacentMeeting != null)
                        {
                            if (meetingTimeChoice.TimeSlotIndex < 2)
                                continue;
                            var linkedMeetingTimeChoice = new MeetingTime(meetingTimeChoice.Day,
                                meetingTimeChoice.TimeSlotIndex - 1);
                            var linkedMeeting = SetMeetingInstance(meetingCopy.RequiredAdjacentMeeting, groupsChoice,
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

        private Meeting? SetMeetingInstance(Meeting meeting, GroupsChoice groupsChoice, MeetingTime meetingTimeChoice)
        {
            var room = TryGetRoomFromPool(meetingTimeChoice.Day, meetingTimeChoice.TimeSlotIndex,
                meeting.RequisitionItem.PlanItem.RoomSpecs);
            if (room == null) return null;
            var meetingCopy = meeting.BasicCopy();
            meetingCopy.Groups = groupsChoice.Groups;
            meetingCopy.MeetingTime = meetingTimeChoice;
            meetingCopy.Location = room;
            if (IsAnyCollision(groupsChoice, meetingTimeChoice, meetingCopy))
                return null;
            return meetingCopy;
        }

        private bool IsAnyCollision(GroupsChoice groupsChoice, MeetingTime meetingTime, Meeting meeting)
        {
            return IsCollisionMeetingToGroup(groupsChoice, meetingTime)
                   || IsOverfillMeetingToGroup(meeting, groupsChoice)
                   || IsCollisionMeetingToTeacher(meeting, meetingTime)
                   || OnlineOfflineDelta(groupsChoice, meetingTime, meeting)
                   || !DoesMeetingTimeSatisfy(meeting, meetingTime);
        }

        private string? TryGetRoomFromPool(DayOfWeek day, int timeSlotIndex, RoomSpec[] roomRequirement)
        {
            if (roomRequirement.Contains(RoomSpec.Online))
                return "Онлайн";
            var possibleRooms = FreeRoomsByDay[day][timeSlotIndex].ToHashSet();
            foreach (var rs in roomRequirement)
                possibleRooms.IntersectWith(RoomsBySpec[rs]);
            return possibleRooms.OrderBy(e => SpecsByRoom[e].Count).FirstOrDefault();
        }

        private bool IsCollisionMeetingToTeacher(Meeting meetingToAdd, MeetingTime meetingTimeChoice)
        {
            var teacher = meetingToAdd.Teacher;
            if (TeacherMeetingsByTime.ContainsKey(teacher) &&
                TeacherMeetingsByTime[teacher].ContainsKey(meetingTimeChoice))
            {
                // Console.WriteLine($"Коллизия у препода {teacher} во время {meetingTimeChoice}, встреча {m}");
                return true;
            }

            return false;
        }

        private bool DoesMeetingTimeSatisfy(Meeting meeting, MeetingTime meetingTime)
        {
            return meeting.RequisitionItem.MeetingTimePriorities
                .Any(timePriority => timePriority.MeetingTimeChoices.Contains(meetingTime));
        }

        private bool OnlineOfflineDelta(GroupsChoice groupsChoice, MeetingTime meetingTime, Meeting meeting)
        {
            for (var dt = -1; dt < 2; dt += 2)
            {
                var time = meetingTime with {TimeSlotIndex = meetingTime.TimeSlotIndex + dt};
                foreach (var (groupName, groupPart) in groupsChoice.Groups)
                {
                    if (!GroupMeetingsByTime.ContainsKey(groupName)) continue;
                    if (groupPart == GroupPart.FullGroup)
                    {
                        if (CheckNeighbourMeeting(meeting, groupName, GroupPart.Part1, time)) return true;
                        if (CheckNeighbourMeeting(meeting, groupName, GroupPart.Part2, time)) return true;
                    }
                    else
                    {
                        if (CheckNeighbourMeeting(meeting, groupName, groupPart, time)) return true;
                    }
                }
            }

            return false;
        }

        private bool CheckNeighbourMeeting(Meeting meeting, string groupName, GroupPart part, MeetingTime meetingTime)
        {
            var (day, timeSlot) = meetingTime;
            if (!GroupMeetingsByTime[groupName].ContainsKey(part) ||
                !GroupMeetingsByTime[groupName][part].ContainsKey(day) ||
                !GroupMeetingsByTime[groupName][part][day].TryGetValue(timeSlot, out var value)) return false;
            return value.RequisitionItem.IsOnline != meeting.RequisitionItem.IsOnline;
        }

        private bool IsOverfillMeetingToGroup(Meeting meetingToAdd, GroupsChoice groupsChoice)
        {
            var planItem = meetingToAdd.RequisitionItem.PlanItem;
            foreach (var (groupName, groupPart) in groupsChoice.Groups)
            {
                if (!GroupLearningPlanItemsCount.ContainsKey(groupName)) continue;
                if (groupPart == GroupPart.FullGroup)
                {
                    if (CheckOverfillMeeting(groupName, planItem, GroupPart.Part1)) return true;
                    if (CheckOverfillMeeting(groupName, planItem, GroupPart.Part2)) return true;
                }
                else
                {
                    if (CheckOverfillMeeting(groupName, planItem, groupPart)) return true;
                }
            }

            return false;
        }

        private bool CheckOverfillMeeting(string groupName, LearningPlanItem planItem, GroupPart groupPart)
        {
            return GroupLearningPlanItemsCount[groupName].ContainsKey(groupPart)
                   && GroupLearningPlanItemsCount[groupName][groupPart].ContainsKey(planItem)
                   && GroupLearningPlanItemsCount[groupName][groupPart][planItem] ==
                   (int) Math.Ceiling(planItem.MeetingsPerWeek);
        }

        private bool IsCollisionMeetingToGroup(GroupsChoice groupsChoice, MeetingTime meetingTime)
        {
            foreach (var (groupName, groupPart) in groupsChoice.Groups)
            {
                if (!GroupMeetingsByTime.ContainsKey(groupName)) continue;
                if (groupPart == GroupPart.FullGroup)
                {
                    if (CheckCollisionMeeting(meetingTime, groupName, GroupPart.Part1)) return true;
                    if (CheckCollisionMeeting(meetingTime, groupName, GroupPart.Part2)) return true;
                }
                else
                {
                    if (CheckCollisionMeeting(meetingTime, groupName, groupPart)) return true;
                }
            }

            return false;
        }

        private bool CheckCollisionMeeting(MeetingTime meetingTime, string groupName, GroupPart groupPart)
        {
            var (day, timeSlotIndex) = meetingTime;
            return GroupMeetingsByTime[groupName].ContainsKey(groupPart)
                   && GroupMeetingsByTime[groupName][groupPart].ContainsKey(day)
                   && GroupMeetingsByTime[groupName][groupPart][day]
                       .ContainsKey(timeSlotIndex);
        }

        private List<Meeting> GetLinkedMeetings(Meeting meeting)
        {
            var meetings = new List<Meeting> {meeting};
            if (meeting.RequiredAdjacentMeeting != null)
                meetings.Add(meeting.RequiredAdjacentMeeting);
            return meetings;
        }


        public void AddMeeting(Meeting meeting)
        {
            var meetings = GetLinkedMeetings(meeting);

            foreach (var meetingToAdd in meetings)
            {
                Meetings.Add(meetingToAdd);

                var meetingTime = meetingToAdd.MeetingTime!;
                TeacherMeetingsByTime.SafeAdd(meetingToAdd.Teacher, meetingTime, meetingToAdd);
                FreeRoomsByDay[meetingTime.Day][meetingTime.TimeSlotIndex].Remove(meetingToAdd.Location!);
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
                    FreeRoomsByDay[meetingTime.Day][meetingTime.TimeSlotIndex].Add(meetingToRemove.Location!);

                RemoveMeetingFromGroup(meetingToRemove, meetingTime);

                TeacherMeetingsTimesByDay[meetingTime.Day][meetingToRemove.Teacher].Remove(meetingTime.TimeSlotIndex);

                NotUsedMeetings.Add(meetingToRemove.BaseMeeting!);
            }
        }
        
        private void AddMeetingToGroup(Meeting meetingToAdd, MeetingTime meetingTime)
        {
            foreach (var (groupName, groupPart) in meetingToAdd.Groups!)
            {
                var planItem = meetingToAdd.RequisitionItem.PlanItem;
                if (groupPart == GroupPart.FullGroup)
                {
                    SafeAddMeetingToGroup(meetingToAdd, meetingTime, groupName, GroupPart.Part1, planItem);
                    SafeAddMeetingToGroup(meetingToAdd, meetingTime, groupName, GroupPart.Part2, planItem);
                }
                else
                {
                    SafeAddMeetingToGroup(meetingToAdd, meetingTime, groupName, groupPart, planItem);
                }
            }
        }

        private void SafeAddMeetingToGroup(Meeting meetingToAdd, MeetingTime meetingTime, string groupName,
            GroupPart groupPart, LearningPlanItem planItem)
        {
            var (day, timeSlotIndex) = meetingTime;
            GroupMeetingsByTime.SafeAdd(groupName, groupPart, day, timeSlotIndex, meetingToAdd);
            GroupLearningPlanItemsCount.SafeIncrement(groupName, groupPart, planItem);
            if (!GroupsMeetingsTimesByDay.ContainsKey(day))
                GroupsMeetingsTimesByDay.Add(day,
                    new Dictionary<string, Dictionary<GroupPart, SortedSet<int>>>());
            GroupsMeetingsTimesByDay[day].SafeAdd(groupName, groupPart, timeSlotIndex);
        }

        private void RemoveMeetingFromGroup(Meeting meetingToRemove, MeetingTime meetingTime)
        {
            foreach (var (groupName, groupPart) in meetingToRemove.Groups!)
            {
                var planItem = meetingToRemove.RequisitionItem.PlanItem;
                if (groupPart == GroupPart.FullGroup)
                {
                    SafeRemoveMeetingFromGroup(meetingTime, groupName, GroupPart.Part1, planItem);
                    SafeRemoveMeetingFromGroup(meetingTime, groupName, GroupPart.Part2, planItem);
                }
                else
                {
                    SafeRemoveMeetingFromGroup(meetingTime, groupName, groupPart, planItem);
                }
            }
        }

        private void SafeRemoveMeetingFromGroup(MeetingTime meetingTime, string groupName, GroupPart groupPart,
            LearningPlanItem planItem)
        {
            var (day, timeSlotIndex) = meetingTime;
            GroupMeetingsByTime[groupName][groupPart][day].Remove(timeSlotIndex);
            GroupLearningPlanItemsCount.SafeDecrement(groupName, groupPart, planItem);
            GroupsMeetingsTimesByDay[day][groupName][groupPart].Remove(timeSlotIndex);
        }

        public IEnumerable<Meeting> GetMeetings()
        {
            return Meetings;
        }
    }
}