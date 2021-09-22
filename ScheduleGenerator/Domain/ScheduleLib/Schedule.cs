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
        public readonly Dictionary<RoomSpec, List<string>> RoomsBySpec = new ();
        public readonly Dictionary<string, Dictionary<GroupPart, Dictionary<MeetingTime, Meeting>>> GroupMeetingsByTime = new();
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
                var linkedMeeting = notUsedMeetings.FirstOrDefault(e => e.Discipline.Equals(meeting.Discipline)
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
                var meetingFound = false;
                var possibleGroupsChoices = requisitionItem.GroupPriorities
                    .SelectMany(p => p.GroupsChoices)
                    .ToHashSet();
                var possibleTimeChoices = requisitionItem.MeetingTimePriorities
                    .SelectMany(p => p.MeetingTimeChoices)
                    .ToHashSet();
                var requiredAdjacentMeetingType = meeting.RequisitionItem.PlanItem.RequiredAdjacentMeetingType;

                foreach (var groupsChoice in possibleGroupsChoices)
                {
                    foreach (var meetingTimeChoice in possibleTimeChoices)
                    {
                        var thisConditionMeetingFound = false;
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
                        // if (requiredAdjacentMeetingType != null && meetingTimeChoice.TimeSlotIndex == 1)
                        //     continue;
                        // TODO Не возвращать митинг с зависимостью, если для зависимости нет места.
                        // TODO Оставлять окно между онлайн и оффлайн парами
                        meetingFound = true;
                        yield return meetingCopy;
                    }
                }

                // if (!meetingFound && meeting.RequisitionItem.PlanItem.RequiredAdjacentMeetingType == null)
                    // throw new FormatException($"Нет свободных мест для пары {meeting}");
            }
        }

        private Meeting? SetMeetingInstance(Meeting meeting, GroupsChoice groupsChoice, MeetingTime? meetingTimeChoice)
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

        private bool IsAnyCollision(GroupsChoice? groupsChoice, MeetingTime? meetingTimeChoice, Meeting? meetingCopy)
        {
            return IsCollisionMeetingToGroup(groupsChoice, meetingTimeChoice)
                   || IsOverfillMeetingToGroup(meetingCopy, groupsChoice)
                   || IsCollisionMeetingToTeacher(meetingCopy, meetingTimeChoice);
        }

        private string? TryGetRoomFromPool(DayOfWeek day, int timeSlotIndex, RoomSpec[] roomRequirement)
        {
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

        private bool IsOverfillMeetingToGroup(Meeting meetingToAdd, GroupsChoice groupsChoice)
        {
            var planItem = meetingToAdd.RequisitionItem.PlanItem;
            foreach (var (groupName, groupPart) in groupsChoice.Groups)
            {
                if (!GroupLearningPlanItemsCount.ContainsKey(groupName)) continue;
                if (groupPart == GroupPart.FullGroup)
                {
                    if (GroupLearningPlanItemsCount[groupName].ContainsKey(GroupPart.Part1)
                        && GroupLearningPlanItemsCount[groupName][GroupPart.Part1].ContainsKey(planItem)
                        && GroupLearningPlanItemsCount[groupName][GroupPart.Part1][planItem] ==
                        (int) Math.Ceiling(planItem.MeetingsPerWeek))
                        return true;
                    if (GroupLearningPlanItemsCount[groupName].ContainsKey(GroupPart.Part2)
                        && GroupLearningPlanItemsCount[groupName][GroupPart.Part2].ContainsKey(planItem)
                        && GroupLearningPlanItemsCount[groupName][GroupPart.Part2][planItem] ==
                        (int) Math.Ceiling(planItem.MeetingsPerWeek))
                        return true;
                }
                else
                {
                    if (GroupLearningPlanItemsCount[groupName].ContainsKey(groupPart)
                        && GroupLearningPlanItemsCount[groupName][groupPart].ContainsKey(planItem)
                        && GroupLearningPlanItemsCount[groupName][groupPart][planItem] ==
                        (int) Math.Ceiling(planItem.MeetingsPerWeek))
                        return true;
                }
            }
            return false;
        }

        private bool IsCollisionMeetingToGroup(GroupsChoice groupsChoice,
            MeetingTime meetingTimeChoice)
        {
            foreach (var (groupName, groupPart) in groupsChoice.Groups)
            {
                if (!GroupMeetingsByTime.ContainsKey(groupName)) continue;
                if (groupPart == GroupPart.FullGroup)
                {
                    if (GroupMeetingsByTime[groupName].ContainsKey(GroupPart.Part1)
                        && GroupMeetingsByTime[groupName][GroupPart.Part1].ContainsKey(meetingTimeChoice))
                        return true;
                    if (GroupMeetingsByTime[groupName].ContainsKey(GroupPart.Part2)
                        && GroupMeetingsByTime[groupName][GroupPart.Part2].ContainsKey(meetingTimeChoice))
                        return true;
                }
                else
                {
                    if (GroupMeetingsByTime[groupName].ContainsKey(groupPart)
                        && GroupMeetingsByTime[groupName][groupPart].ContainsKey(meetingTimeChoice))
                        return true;
                }
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

        private void AddMeetingToGroup(Meeting meetingToAdd, MeetingTime meetingTime)
        {
            foreach (var (groupName, groupPart) in meetingToAdd.Groups!)
            {
                var planItem = meetingToAdd.RequisitionItem.PlanItem;
                if (groupPart == GroupPart.FullGroup)
                {
                    GroupMeetingsByTime.SafeAdd(groupName, GroupPart.Part1, meetingTime, meetingToAdd);
                    GroupMeetingsByTime.SafeAdd(groupName, GroupPart.Part2, meetingTime, meetingToAdd);
                    GroupLearningPlanItemsCount.SafeIncrement(groupName, GroupPart.Part1, planItem);
                    GroupLearningPlanItemsCount.SafeIncrement(groupName, GroupPart.Part2, planItem);
                    if (!GroupsMeetingsTimesByDay.ContainsKey(meetingTime.Day))
                        GroupsMeetingsTimesByDay.Add(meetingTime.Day, new Dictionary<string, Dictionary<GroupPart, SortedSet<int>>>());
                    GroupsMeetingsTimesByDay[meetingTime.Day].SafeAdd(groupName,GroupPart.Part1,meetingTime.TimeSlotIndex);
                    GroupsMeetingsTimesByDay[meetingTime.Day].SafeAdd(groupName,GroupPart.Part2,meetingTime.TimeSlotIndex);
                }
                else
                {
                    GroupMeetingsByTime.SafeAdd(groupName, groupPart, meetingTime, meetingToAdd);
                    GroupLearningPlanItemsCount.SafeIncrement(groupName, groupPart, planItem);
                    if (!GroupsMeetingsTimesByDay.ContainsKey(meetingTime.Day))
                        GroupsMeetingsTimesByDay.Add(meetingTime.Day, new Dictionary<string, Dictionary<GroupPart, SortedSet<int>>>());
                    GroupsMeetingsTimesByDay[meetingTime.Day].SafeAdd(groupName,groupPart,meetingTime.TimeSlotIndex);
                }
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
                FreeRoomsByDay[meetingTime.Day][meetingTime.TimeSlotIndex].Add(meetingToRemove.Location!);
            
                RemoveMeetingFromGroup(meetingToRemove, meetingTime);

                TeacherMeetingsTimesByDay[meetingTime.Day][meetingToRemove.Teacher].Remove(meetingTime.TimeSlotIndex);

                NotUsedMeetings.Add(meetingToRemove.BaseMeeting!);
            }
        }
        
        private void RemoveMeetingFromGroup(Meeting meetingToRemove, MeetingTime meetingTime)
        {
            foreach (var (groupName, groupPart) in meetingToRemove.Groups!)
            {
                var planItem = meetingToRemove.RequisitionItem.PlanItem;
                if (groupPart == GroupPart.FullGroup)
                {
                    GroupMeetingsByTime[groupName][GroupPart.Part1].Remove(meetingTime);
                    GroupMeetingsByTime[groupName][GroupPart.Part2].Remove(meetingTime);
                    GroupLearningPlanItemsCount.SafeDecrement(groupName, GroupPart.Part1, planItem);
                    GroupLearningPlanItemsCount.SafeDecrement(groupName, GroupPart.Part2, planItem);
                    GroupsMeetingsTimesByDay[meetingTime.Day][groupName][GroupPart.Part1].Remove(meetingTime.TimeSlotIndex);
                    GroupsMeetingsTimesByDay[meetingTime.Day][groupName][GroupPart.Part2].Remove(meetingTime.TimeSlotIndex);
                }
                else
                {
                    GroupMeetingsByTime[groupName][groupPart].Remove(meetingTime);
                    GroupLearningPlanItemsCount.SafeDecrement(groupName, groupPart, planItem);
                    GroupsMeetingsTimesByDay[meetingTime.Day][groupName][groupPart].Remove(meetingTime.TimeSlotIndex);
                }
            }
        }

        public IEnumerable<Meeting> GetMeetings()
        {
            return Meetings;
        }
    }
}