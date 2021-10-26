using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Algorithms;
using Domain.Conversions;
using Domain.Enums;
using Domain.MeetingsParts;

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
        public readonly Dictionary<string, List<RoomSpec>> SpecsByRoom;
        public readonly Dictionary<RoomSpec, List<string>> RoomsBySpec = new();

        public readonly Dictionary<MeetingGroup, Dictionary<WeekType, Dictionary<DayOfWeek, Meeting?[]>>>
            GroupMeetingsByTime = new();

        public readonly Dictionary<Teacher, Dictionary<WeekType, Dictionary<DayOfWeek, Meeting?[]>>>
            TeacherMeetingsByTime = new();

        public readonly Dictionary<MeetingGroup, Dictionary<LearningPlanItem, double>> GroupLearningPlanItemsCount =
            new();

        public readonly Dictionary<MeetingTime, HashSet<string>> FreeRoomsByDay = new();

        public readonly Dictionary<MeetingTime, HashSet<Meeting>> MeetingsByTimeSlot = new();
        public readonly Dictionary<Meeting, int> FreeTimeSlotsCountByMeeting = new();

        public readonly Dictionary<Meeting, int> MeetingFreedomDegree = new();

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
                var possibleTimeChoices = meeting.RequisitionItem.MeetingTimePriorities
                    .SelectMany(p => p.MeetingTimeChoices)
                    .ToHashSet();
                FreeTimeSlotsCountByMeeting.Add(meeting, possibleTimeChoices.Count);
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

        private void UpdateTimeToMeetingsDictionaries(MeetingTime time, int dt)
        {
            foreach (var meeting in MeetingsByTimeSlot[time]) FreeTimeSlotsCountByMeeting[meeting] += dt;
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

                if (isSure)
                {
                    UpdateTimeToMeetingsDictionaries(meetingTime, -1);
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

                if (meetingToRemove.Classroom != null)
                    FreeRoomsByDay[meetingTime].Add(meetingToRemove.Classroom);

                RemoveMeetingFromGroup(meetingToRemove);

                NotUsedMeetings.Add(meetingToRemove.BaseMeeting!);
                if (isSure)
                {
                    UpdateTimeToMeetingsDictionaries(meetingTime, 1);
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
                    throw new ArgumentException(meeting.ToString());
                meeting.RequiredAdjacentMeeting = linkedMeeting;
                linkedMeeting.RequiredAdjacentMeeting = meeting;
            }
        }

        private void FillMeetingFreedomDegree(IEnumerable<Meeting> meetings)
        {
            foreach (var meeting in meetings)
            {
                var possibleRooms = SpecsByRoom.Keys.ToHashSet();
                if (meeting.IsRoomNeeded)
                    foreach (var roomSpec in meeting.RequisitionItem.PlanItem.RoomSpecs)
                        possibleRooms.IntersectWith(RoomsBySpec[roomSpec]);

                var timeChoicesCount = FreeTimeSlotsCountByMeeting[meeting];
                var weektypeDegree = meeting.WeekType == WeekType.OddOrEven ? 2 : 1;
                //TODO: Optimize possibleRooms
                MeetingFreedomDegree[meeting] = timeChoicesCount * possibleRooms.Count * weektypeDegree;
                //MeetingFreedomDegree[meeting] = timeChoicesCount;
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
                string? room = null;
                if (baseMeeting.IsRoomNeeded)
                    room = FindFreeRoom(meetingTime, baseMeeting.RequisitionItem.PlanItem.RoomSpecs);
                if (room == null && baseMeeting.IsRoomNeeded) return null;

                meetingCopy.Groups = groupsChoice.Groups;
                meetingCopy.MeetingTime = meetingTime;
                meetingCopy.Classroom = room;
                if (IsMeetingValid(meetingCopy)) return meetingCopy;
            }

            return null;
        }

        private bool IsMeetingValid(Meeting meeting)
        {
            return !(HasMeetingAlreadyAtThisTime(meeting)
                     || IsMeetingIsExtraForGroup(meeting)
                     || TeacherHasMeetingAlreadyAtThisTime(meeting)
                     || IsNoSpaceBetweenDifferentLocatedMeetings(meeting)
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
            var timeSlotIndex = meeting.MeetingTime!.TimeSlotIndex;
            return TeacherMeetingsByTime.GetDaysByMeeting(teacher, meeting)
                .HasMeetingsAtTime(timeSlotIndex);
        }

        private bool IsTimeAcceptableForTeacher(Meeting meeting)
        {
            var meetingTime = meeting.MeetingTime;
            return meeting.RequisitionItem.MeetingTimePriorities
                .Any(timePriority => timePriority.MeetingTimeChoices.Contains(meetingTime));
        }

        private bool HasMeetingAlreadyAtThisTime(Meeting meeting)
        {
            var timeSlotIndex = meeting.MeetingTime!.TimeSlotIndex;
            return meeting.Groups!.GetGroupParts()
                .SelectMany(g => GroupMeetingsByTime.GetDaysByMeeting(g, meeting))
                .HasMeetingsAtTime(timeSlotIndex);
        }

        private bool IsNoSpaceBetweenDifferentLocatedMeetings(Meeting meeting)
        {
            var timeSlotIndex = meeting.MeetingTime!.TimeSlotIndex;
            var location = meeting.Location;
            foreach (var group in meeting.Groups!.GetGroupParts())
            foreach (var day in GroupMeetingsByTime.GetDaysByMeeting(group, meeting))
                for (var i = -1; i <= 1; i += 2)
                {
                    var timeSlot = timeSlotIndex + i;
                    if (timeSlot is not (> 0 and < 7)) continue;
                    if (day[timeSlot] == null) continue;
                    if (day[timeSlot]!.Location != location) return true;
                }

            return false;
        }

        private bool IsMeetingIsExtraForGroup(Meeting meetingToAdd)
        {
            return meetingToAdd.Groups!.GetGroupParts()
                .Any(g => IsPlanItemFulfilled(g, meetingToAdd.RequisitionItem.PlanItem,
                    meetingToAdd.WeekType == WeekType.All ? 1 : 0.5));
        }

        private bool IsPlanItemFulfilled(MeetingGroup group, LearningPlanItem planItem, double value)
        {
            return GroupLearningPlanItemsCount.ContainsKey(group)
                   && GroupLearningPlanItemsCount[group].ContainsKey(planItem)
                   && GroupLearningPlanItemsCount[group][planItem] + value > planItem.MeetingsPerWeek;
            //TODO pe: это неверно в общем случае. Может быть поставлено три мигающих пары, что в сумме даст 1.5 пары в неделю.
        }

        private void AddMeetingToGroup(Meeting meetingToAdd)
        {
            foreach (var meetingGroup in meetingToAdd.Groups!.GetGroupParts())
            {
                var value = meetingToAdd.WeekType == WeekType.All ? 1 : 0.5;
                GroupMeetingsByTime.SafeAdd(meetingGroup, meetingToAdd);
                GroupLearningPlanItemsCount.SafeIncrement(meetingGroup, meetingToAdd.RequisitionItem.PlanItem, value);
            }
        }

        private void RemoveMeetingFromGroup(Meeting meetingToRemove)
        {
            var (day, timeSlotIndex) = meetingToRemove.MeetingTime!;
            foreach (var meetingGroup in meetingToRemove.Groups!.GetGroupParts())
            {
                foreach (var weekType in meetingToRemove.WeekType.GetWeekTypes())
                    GroupMeetingsByTime[meetingGroup][weekType][day][timeSlotIndex] = null;

                var value = meetingToRemove.WeekType == WeekType.All ? 1 : 0.5;
                GroupLearningPlanItemsCount.SafeDecrement(meetingGroup, meetingToRemove.RequisitionItem.PlanItem,
                    value);
            }
        }
    }
}