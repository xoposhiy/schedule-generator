using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Algorithms;
using Domain.Conversions;

namespace Domain.ScheduleLib
{
    public class Schedule
    {
        public readonly Requisition Requisition;
        public List<Meeting> Meetings = new();
        public List<Meeting> NotUsedMeetings = new();
        public Dictionary<string, List<RoomSpec>> SpecsByRoom;
        public Dictionary<RoomSpec, List<string>> RoomsBySpec = new ();
        public Dictionary<string, Dictionary<MeetingTime, Meeting>> RoomMeetingsByTime = new();
        public Dictionary<string, Dictionary<GroupPart, Dictionary<MeetingTime, Meeting>>> GroupMeetingsByTime = new();
        public Dictionary<string, Dictionary<GroupPart, Dictionary<LearningPlanItem, int>>> G = new();
        public Dictionary<Teacher, Dictionary<MeetingTime, Meeting>> TeacherMeetingsByTime = new();

        public Schedule(Requisition requisition, Dictionary<string, List<RoomSpec>> roomsWithSpecs)
        {
            Requisition = requisition;
            SpecsByRoom = roomsWithSpecs;
            FillClassroomsBySpec(roomsWithSpecs);
            NotUsedMeetings = requisition.Items
                .SelectMany(RequisitionToMeetingConverter.ConvertRequisitionToBasicMeeting)
                .ToList();
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

        public IEnumerable<List<Meeting>> GetMeetingsToAdd()
        {
            //TODO предложить все возможные варианты следующего митинга, который нужно вставить в расписание, с учетом всех жестких ограничений
            foreach (var meeting in NotUsedMeetings.ToList())
            {
                var requisitionItem = meeting.RequisitionItem;
                var meetingFound = false;
                var possibleGroupsChoices =
                    requisitionItem.GroupPriorities.SelectMany(p => p.GroupsChoices).ToHashSet();
                var possibleTimeChoices = requisitionItem.MeetingTimePriorities
                    .SelectMany(p => p.MeetingTimeChoices)
                    .ToHashSet();
                var possibleRooms = new HashSet<string>();
                foreach (var roomSpec in requisitionItem.PlanItem.RoomSpecs)
                {
                    if (roomSpec == RoomSpec.Any)
                        possibleRooms = SpecsByRoom.Select(e => e.Key).ToHashSet();
                    else
                    {
                        foreach (var room in RoomsBySpec[roomSpec])
                        {
                            possibleRooms.Add(room);
                        }
                    }
                }

                foreach (var groupsChoice in possibleGroupsChoices)
                {
                    foreach (var meetingTimeChoice in possibleTimeChoices)
                    {
                        foreach (var roomChoice in possibleRooms)
                        {
                            var thisConditionMeetingFound = false;
                            var meetingsToAdd = new List<Meeting>();
                            var meetingCopy = meeting.BasicCopy();
                            meetingCopy.Groups = groupsChoice.Groups;
                            meetingCopy.MeetingTime = meetingTimeChoice;
                            meetingCopy.Location = roomChoice;
                            var connectAfter = meetingCopy.RequisitionItem.PlanItem.ConnectAfter;
                            if (connectAfter != null)
                            {
                                if (meetingTimeChoice.TimeSlotIndex == 1)
                                    continue;
                                var discipline = meetingCopy.RequisitionItem.PlanItem.Discipline;
                                var sameTeacherWith = meetingCopy.RequisitionItem.PlanItem.SameTeacherWith;
                                var m = Meetings
                                    .FirstOrDefault(m => m.RequisitionItem.PlanItem.Discipline == discipline
                                                         && m.RequisitionItem.PlanItem.MeetingType == connectAfter
                                                         && m.GroupsEquals(meetingCopy.Groups)
                                                         && (sameTeacherWith == null ||
                                                             meetingCopy.Teacher == m.Teacher)
                                                         && m.MeetingTime!.TimeSlotIndex < 6
                                                         && meetingTimeChoice.Day == m.MeetingTime.Day
                                                         && meetingTimeChoice.TimeSlotIndex ==
                                                         m.MeetingTime.TimeSlotIndex + 1);
                                if (m != null)
                                {
                                    thisConditionMeetingFound = true;
                                    meetingFound = true;
                                    meetingCopy.LinkedMeeting = m;
                                    m.LinkedMeeting = meetingCopy;
                                    meetingsToAdd.Add(meetingCopy);
                                }
                                else
                                {
                                    if (connectAfter == meetingCopy.MeetingType)
                                    {
                                        thisConditionMeetingFound = true;
                                        meetingFound = true;
                                        meetingsToAdd.Add(meetingCopy);
                                    }
                                }
                            }
                            else
                            {
                                thisConditionMeetingFound = true;
                                meetingFound = true;
                                meetingsToAdd.Add(meetingCopy);
                            }

                            if (!thisConditionMeetingFound 
                                || IsCollisionMeetingsToGroup(groupsChoice, meetingTimeChoice)
                                || IsOverfillMeetingsToGroup(meetingsToAdd, groupsChoice)
                                || IsCollisionMeetingsToTeacher(meetingsToAdd, meetingTimeChoice)
                                || IsCollisionMeetingsToRoom(meetingsToAdd, meetingTimeChoice))
                                continue;
                            // Console.WriteLine(string.Join(" ", meetingsToAdd));
                            yield return meetingsToAdd;
                        }
                    }
                }

                if (!meetingFound && meeting.RequisitionItem.PlanItem.ConnectAfter == null)
                    throw new FormatException($"Нет свободных мест для пары {meeting}");
            }
        }

        private bool IsCollisionMeetingsToRoom(IEnumerable<Meeting> meetingsToAdd, MeetingTime meetingTimeChoice)
        {
            foreach (var m in meetingsToAdd)
            {
                var room = m.Location!;
                if (RoomMeetingsByTime.ContainsKey(room) && RoomMeetingsByTime[room].ContainsKey(meetingTimeChoice))
                {
                    // Console.WriteLine($"Коллизия у аудитории {room} во время {meetingTimeChoice}, встреча {m}");
                    return true;
                }
            }
            return false;
        }

        private bool IsCollisionMeetingsToTeacher(IEnumerable<Meeting> meetingsToAdd, MeetingTime meetingTimeChoice)
        {
            foreach (var m in meetingsToAdd)
            {
                var teacher = m.Teacher;
                if (TeacherMeetingsByTime.ContainsKey(teacher) && TeacherMeetingsByTime[teacher].ContainsKey(meetingTimeChoice))
                {
                    // Console.WriteLine($"Коллизия у препода {teacher} во время {meetingTimeChoice}, встреча {m}");
                    return true;
                }
            }
            return false;
        }
        
        private bool IsOverfillMeetingsToGroup(IEnumerable<Meeting> meetingsToAdd, GroupsChoice groupsChoice)
        {
            foreach (var m in meetingsToAdd)
            {
                var planItem = m.RequisitionItem.PlanItem;
                foreach (var (groupName, groupPart) in groupsChoice.Groups)
                {
                    if (!G.ContainsKey(groupName)) continue;
                    if (groupPart == GroupPart.FullGroup)
                    {
                        if (G[groupName].ContainsKey(GroupPart.Part1)
                            && G[groupName][GroupPart.Part1].ContainsKey(planItem) 
                            && G[groupName][GroupPart.Part1][planItem] == (int)Math.Ceiling(planItem.MeetingsPerWeek))
                            return true;
                        if (G[groupName].ContainsKey(GroupPart.Part2)
                            && G[groupName][GroupPart.Part2].ContainsKey(planItem)
                            && G[groupName][GroupPart.Part2][planItem] == (int)Math.Ceiling(planItem.MeetingsPerWeek))
                            return true;
                    }
                    else
                    {
                        if (G[groupName].ContainsKey(groupPart)
                            && G[groupName][groupPart].ContainsKey(planItem)
                            && G[groupName][groupPart][planItem] == (int)Math.Ceiling(planItem.MeetingsPerWeek))
                            return true;
                    }
                }
            }
            return false;
        }

        private bool IsCollisionMeetingsToGroup(GroupsChoice groupsChoice,
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


        public void AddMeeting(Meeting meetingToAdd)
        {
            Meetings.Add(meetingToAdd);
            
            var meetingTime = meetingToAdd.MeetingTime!;
            TeacherMeetingsByTime.SafeAdd(meetingToAdd.Teacher, meetingTime, meetingToAdd);
            RoomMeetingsByTime.SafeAdd(meetingToAdd.Location!, meetingTime, meetingToAdd);
            AddMeetingToGroup(meetingToAdd, meetingTime);
            
            NotUsedMeetings.Remove(meetingToAdd.BaseMeeting!);
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
                    G.SafeIncrement(groupName, GroupPart.Part1, planItem);
                    G.SafeIncrement(groupName, GroupPart.Part2, planItem);
                }
                else
                {
                    GroupMeetingsByTime.SafeAdd(groupName, groupPart, meetingTime, meetingToAdd);
                    G.SafeIncrement(groupName, groupPart, planItem);
                }
            }
        }


        public void RemoveMeeting(Meeting meetingToRemove)
        {
            Meetings.Remove(meetingToRemove);
            
            var meetingTime = meetingToRemove.MeetingTime!;
            TeacherMeetingsByTime[meetingToRemove.Teacher].Remove(meetingTime);
            RoomMeetingsByTime[meetingToRemove.Location!].Remove(meetingTime);
            RemoveMeetingFromGroup(meetingToRemove, meetingTime);
            
            NotUsedMeetings.Add(meetingToRemove.BaseMeeting!);
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
                    G.SafeDecrement(groupName, GroupPart.Part1, planItem);
                    G.SafeDecrement(groupName, GroupPart.Part2, planItem);
                }
                else
                {
                    GroupMeetingsByTime[groupName][groupPart].Remove(meetingTime);
                    G.SafeDecrement(groupName, GroupPart.Part1, planItem);
                }
            }
        }
    }
}