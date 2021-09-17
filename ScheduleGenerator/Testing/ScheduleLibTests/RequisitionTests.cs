using System;
using Domain.ScheduleLib;
using NUnit.Framework;

namespace Testing.ScheduleLibTests
{
    [TestFixture]
    public class RequisitionTests
    {
        [Test]
        public void TestGroupMaskInit()
        {
            var discipline = new Discipline("Test");
            var roomSpecs = new[] {RoomSpec.Big, RoomSpec.Projector};
            var planItem = new LearningPlanItem("ФИИТ-666", discipline, MeetingType.Lecture, GroupSize.FullGroup, 1,
                roomSpecs, null, null);
            
            var meetingGroup = new MeetingGroup("Test", GroupPart.FullGroup);
            var groupChoice = new GroupsChoice(new[] {meetingGroup});
            var groupRequisition = new GroupRequisition(new[] {groupChoice});
            
            var metingTime = new MeetingTime(DayOfWeek.Monday, 2);
            var meetingTimeRequisition = new MeetingTimeRequisition(new[] {metingTime});
            
            var teacher = new Teacher("God");
            
            var requisitionItem = new RequisitionItem(planItem, new[] {groupRequisition}, 1,
                new[] {meetingTimeRequisition}, teacher, WeekType.All);
            
            Assert.Pass();
        }
    }
}