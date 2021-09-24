using System;
using System.Linq;
using Domain.ScheduleLib;
using NUnit.Framework;
using NUnit.Framework.Constraints;

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
            Assert.That(schedule.GetMeetingsToAdd().Select(m => m.Location), Has.All.EqualTo("Онлайн"));
        }
        //TODO: Проверить, что между онлайн парами и оффлайн достаточно перерыва
    }
}