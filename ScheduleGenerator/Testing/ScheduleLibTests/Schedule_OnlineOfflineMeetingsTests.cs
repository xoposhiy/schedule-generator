using System;
using System.Linq;
using Domain.ScheduleLib;
using NUnit.Framework;

namespace Testing.ScheduleLibTests
{
    [TestFixture]
    public class Schedule_OnlineOfflineMeetingsTests
    {
        [Test]
        public void TestOnlineLocation()
        {
            var requisition = new Requisition(new[]
            {
                new RequisitionItem(ObjectMother.CalculusLecture, "ФИИТ-101", "пн 1-6 пара",
                    ObjectMother.CalculusTeacher, 1, WeekType.All, true),
                new RequisitionItem(ObjectMother.CalculusSeminar, "ФИИТ-101", "пн 1-6 пара",
                    ObjectMother.CalculusTeacher, 1, WeekType.All, false),
            });

            var schedule = new Schedule(requisition, ObjectMother.ClassRooms);
            foreach (var meeting in schedule.GetMeetingsToAdd())
            {
                Console.Error.WriteLine(meeting);
            }

            var lecturePossibleLocations = schedule.GetMeetingsToAdd().Where(m => m.MeetingType == MeetingType.Lecture).Select(m => m.Location);
            Assert.That(lecturePossibleLocations, Has.All.EqualTo(Meeting.OnlineLocationName));
        }
        //TODO: Проверить, что между онлайн парами и оффлайн достаточно перерыва
    }
}