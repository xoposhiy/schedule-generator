using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Conversions;
using Domain.Enums;
using Domain.MeetingsParts;
using static Domain.DomainExtensions;

namespace Domain
{
    public record ScheduleTime(DayOfWeek Day, int TimeSlot, WeekType WeekType);

    public record ScheduleDay(DayOfWeek Day, WeekType Week);

    public interface IReadonlySchedule
    {
        IReadOnlySet<Meeting> GetMeetings();
    }

    public class Schedule : IReadonlySchedule
    {
        public readonly HashSet<Meeting> Meetings = new();
        public readonly HashSet<Meeting> NotUsedMeetings;
        public readonly HashSet<Meeting> NonPlaceableMeetings = new();
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

        private readonly Dictionary<MeetingGroup, Dictionary<MeetingTime, Dictionary<WeekType, HashSet<Meeting>>>>
            timeConcurrentMeetings = new();

        public readonly Dictionary<Meeting, int> MeetingFreedomDegree = new();

        public readonly Dictionary<MeetingGroup,
            Dictionary<Discipline, Dictionary<MeetingType, Dictionary<Teacher, double>>>> GroupTeachersByDiscipline =
            new();

        public Schedule(Requisition requisition, Dictionary<string, List<RoomSpec>> specsByRoom)
        {
            FillTeachersKeys(requisition);
            FillGroupsKeys(requisition);

            SpecsByRoom = specsByRoom;
            FillClassroomsBySpec(specsByRoom);
            FillRoomPool(specsByRoom.Keys);
            NotUsedMeetings = requisition.Items
                .SelectMany(RequisitionToMeetingConverter.ConvertRequisitionToBasicMeeting)
                .ToHashSet();
            LinkBasicMeetings(NotUsedMeetings);
            FillTimeToMeetingsDictionaries(NotUsedMeetings);
        }

        private void FillGroupsKeys(Requisition requisition)
        {
            var groups = requisition.Items
                .SelectMany(r => r.GroupPriorities)
                .SelectMany(g => g.GroupsChoices)
                .SelectMany(g => g.Groups.GetGroupParts())
                .Distinct();

            foreach (var group in groups)
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
                var possibleTimeChoices = meeting.RequisitionItem.MeetingTimePriorities
                    .SelectMany(p => p.MeetingTimeChoices)
                    .ToHashSet();
                var groups = meeting.RequisitionItem.GroupPriorities
                    .SelectMany(g => g.GroupsChoices)
                    .SelectMany(g => g.Groups.GetGroupParts())
                    .ToHashSet();

                WeekType[] weekTypes = meeting.WeekType is WeekType.All or WeekType.OddOrEven
                    ? new[] {WeekType.Odd, WeekType.Even}
                    : new[] {meeting.WeekType};

                foreach (var group in groups)
                foreach (var time in possibleTimeChoices)
                foreach (var weekType in weekTypes)
                {
                    timeConcurrentMeetings.SafeAdd(group, time, weekType, meeting);
                }

                var weekTypeDegree = meeting.WeekType == WeekType.OddOrEven ? 2 : 1;
                var groupDegree = meeting.RequisitionItem.GroupPriorities.Sum(g => g.GroupsChoices.Length);
                MeetingFreedomDegree.Add(meeting, possibleTimeChoices.Count * weekTypeDegree * groupDegree);

                foreach (var timeChoice in possibleTimeChoices)
                {
                    MeetingsByTimeSlot.SafeAdd(timeChoice, meeting);
                }
            }
        }

        public IReadOnlySet<Meeting> GetMeetings()
        {
            return Meetings;
        }

        private void UnsubscribeCollidingMeetings(Meeting meeting)
        {
            var time = meeting.MeetingTime!;
            foreach (var group in meeting.Groups!.GetGroupParts())
            foreach (var weekType in meeting.WeekType.GetWeekTypes())
            foreach (var concurrentMeeting in timeConcurrentMeetings[group][time][weekType].ToList())
                UnsubscribeMeetingFromCell(concurrentMeeting, group, time, weekType);
        }

        private void ResetMeetingsSubscriptions()
        {
            // var busyCells = GetAllBusyCells();
            // if (timeConcurrentMeetings[group][time][weekType].Count == 0) <- Placed

            throw new NotImplementedException("Subscription needed");

            // TODO krutovsky: Subscribe meeting to dict
            // Copy paste FillTimeToMeetingsDictionaries
        }

        private void UnsubscribeMeetingFromCell(Meeting collidingMeeting, MeetingGroup meetingGroup, MeetingTime time,
            WeekType weekType)
        {
            WeekType[] weekTypes = collidingMeeting.WeekType == WeekType.All
                ? new[] {WeekType.Odd, WeekType.Even}
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

                if (isSure)
                {
                    UnsubscribeCollidingMeetings(meetingToAdd);
                }
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
                if (isSure)
                {
                    ResetMeetingsSubscriptions();
                }
            }
        }

        public IEnumerable<Meeting> GetMeetingsToAdd()
        {
            var placeableMeetings = NotUsedMeetings
                .Where(m => !NonPlaceableMeetings.Contains(m))
                // .Where(m => MeetingFreedomDegree[m] > 0)
                .ToList();
            if (placeableMeetings.Count == 0)
                yield break;
            var maxPriority = placeableMeetings.Max(m => m.Priority);
            var priorityMeetings = placeableMeetings
                .Where(m => m.Priority == maxPriority)
                .ToList();
            var minFreedomDegree = priorityMeetings.Min(m => MeetingFreedomDegree[m]);
            // Console.WriteLine($"Min Freedom: {minFreedomDegree}");
            var minFreedomMeetings = priorityMeetings
                .Where(m => MeetingFreedomDegree[m] == minFreedomDegree)
                .ToList();

            var placeableMeetingsCount = 0;
            foreach (var baseMeeting in minFreedomMeetings)
            {
                var requisitionItem = baseMeeting.RequisitionItem;
                var possibleGroupsChoices = requisitionItem.GroupPriorities
                    .SelectMany(p => p.GroupsChoices);
                var possibleTimeChoices = requisitionItem.MeetingTimePriorities
                    .SelectMany(p => p.MeetingTimeChoices)
                    .ToHashSet();

                var meetingVariants = 0;

                foreach (var groupsChoice in possibleGroupsChoices)
                foreach (var meetingTimeChoice in possibleTimeChoices)
                {
                    var meetingCopy = TryCreateFilledMeeting(baseMeeting, groupsChoice, meetingTimeChoice);
                    if (meetingCopy == null) continue;
                    if (meetingCopy.RequiredAdjacentMeeting != null)
                    {
                        if (meetingTimeChoice.TimeSlot < 2)
                            continue;
                        var linkedMeetingTimeChoice = new MeetingTime(meetingTimeChoice.Day,
                            meetingTimeChoice.TimeSlot - 1);
                        var linkedMeeting = TryCreateFilledMeeting(meetingCopy.RequiredAdjacentMeeting,
                            groupsChoice,
                            linkedMeetingTimeChoice);

                        if (linkedMeeting == null) continue;
                        LinkMeetings(meetingCopy, linkedMeeting);
                    }

                    meetingVariants++;
                    placeableMeetingsCount++;
                    yield return meetingCopy;
                }

                if (meetingVariants == 0)
                    NonPlaceableMeetings.Add(baseMeeting);
                // NotUsedMeetings.Remove(baseMeeting);
            }

            if (placeableMeetingsCount != 0) yield break;
            foreach (var meeting in GetMeetingsToAdd()) yield return meeting;
        }

        private static void LinkBasicMeetings(HashSet<Meeting> notUsedMeetings)
        {
            foreach (var meeting in notUsedMeetings)
            {
                var requiredAdjacentMeetingType = meeting.PlanItem.RequiredAdjacentMeetingType;
                if (requiredAdjacentMeetingType == null) continue;
                var linkedMeeting = notUsedMeetings
                    .FirstOrDefault(e => e.Discipline.Equals(meeting.Discipline)
                                         && e.Teacher.Equals(meeting.Teacher)
                                         && e.MeetingType.Equals(requiredAdjacentMeetingType)
                                         && !ReferenceEquals(e, meeting));
                if (linkedMeeting == null)
                    throw new ArgumentException(meeting.ToString());
                LinkMeetings(meeting, linkedMeeting);
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
            var meetingCopy = baseMeeting.BasicCopy();
            meetingCopy.Groups = groupsChoice.Groups;
            meetingCopy.MeetingTime = meetingTime;

            if (baseMeeting.IsRoomNeeded)
            {
                var room = FindFreeRoom(meetingTime, baseMeeting.PlanItem.RoomSpecs);
                if (room == null) return null;
                meetingCopy.Classroom = room;
            }

            WeekType[] weekTypes = baseMeeting.WeekType == WeekType.OddOrEven
                ? new[] {WeekType.Odd, WeekType.Even}
                : new[] {baseMeeting.WeekType};

            foreach (var weekType in weekTypes)
            {
                meetingCopy.WeekType = weekType;

                if (IsMeetingValid(meetingCopy)) return meetingCopy;
            }

            return null;
        }

        private bool IsMeetingValid(Meeting meeting)
        {
            return !(HasMeetingAlreadyAtThisTime(meeting) // weekType requires
                     || IsMeetingIsExtraForGroup(meeting)
                     || TeacherHasMeetingAlreadyAtThisTime(meeting) // weekType requires
                     || IsNoSpaceBetweenDifferentLocatedMeetings(meeting) // weekType requires
                     || !IsTimeAcceptableForTeacher(meeting)
                     || IsTeacherIsExtraForGroup(meeting)
                );
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
            var timeSlot = meeting.MeetingTime!.TimeSlot;
            return TeacherMeetingsByTime.GetDaysByMeeting(teacher, meeting)
                .HasMeetingsAtTime(timeSlot);
        }

        private static bool IsTimeAcceptableForTeacher(Meeting meeting)
        {
            var meetingTime = meeting.MeetingTime;
            return meeting.RequisitionItem.MeetingTimePriorities
                .Any(timePriority => timePriority.MeetingTimeChoices.Contains(meetingTime));
        }

        private bool HasMeetingAlreadyAtThisTime(Meeting meeting)
        {
            var timeSlot = meeting.MeetingTime!.TimeSlot;
            return meeting.Groups!.GetGroupParts()
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
            foreach (var group in meeting.Groups!.GetGroupParts())
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
            foreach (var meetingGroup in meetingToAdd.Groups!.GetGroupParts())
            {
                if (!GroupLearningPlanItemsCount.TryGetValue(meetingGroup,
                    out var byGroup)) continue;
                if (!byGroup.TryGetValue(planItem, out var weight)) continue;
                if (weight + additionalWeight > planItem.MeetingsPerWeek) return true;
            }

            return false;
        }

        private bool IsTeacherIsExtraForGroup(Meeting meetingToAdd)
        {
            foreach (var meetingGroup in meetingToAdd.Groups!.GetGroupParts())
            {
                if (!GroupTeachersByDiscipline.TryGetValue(meetingGroup,
                    out var byGroup)) continue;
                if (!byGroup.TryGetValue(meetingToAdd.Discipline,
                    out var byDiscipline)) continue;
                if (!byDiscipline.TryGetValue(meetingToAdd.MeetingType,
                    out var byType)) continue;
                if (byType.Any(teacher => teacher.Value > 0
                                          && teacher.Key != meetingToAdd.Teacher))
                    return true;
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
            foreach (var meetingGroup in meetingToAdd.Groups!.GetGroupParts())
            {
                GroupMeetingsByTime.SafeAdd(meetingGroup, meetingToAdd);
                GroupLearningPlanItemsCount.SafeIncrement(meetingGroup, planItem, value);

                if (!GroupTeachersByDiscipline[meetingGroup].ContainsKey(discipline))
                    GroupTeachersByDiscipline[meetingGroup].Add(discipline, new());

                GroupTeachersByDiscipline[meetingGroup][discipline].SafeIncrement(meetingType, teacher, value);
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
            foreach (var meetingGroup in meetingToRemove.Groups!.GetGroupParts())
            {
                foreach (var weekType in meetingToRemove.WeekType.GetWeekTypes())
                    GroupMeetingsByTime[meetingGroup][weekType][day][timeSlot] = null;

                GroupLearningPlanItemsCount.SafeDecrement(meetingGroup, planItem, value);
                GroupTeachersByDiscipline[meetingGroup][discipline][meetingType][teacher] -= value;
            }
        }
    }
}