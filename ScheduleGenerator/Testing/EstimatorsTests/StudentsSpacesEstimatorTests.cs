using System.Linq;
using Domain;
using Domain.Algorithms;
using Domain.Algorithms.Estimators;
using NUnit.Framework;
using static Testing.ScheduleLibTests.ObjectMother;

namespace Testing.EstimatorsTests
{
    [TestFixture]
    public class StudentsSpacesEstimatorTests
    {
        private readonly IEstimator estimator = new StudentsSpacesEstimator();

        [Test]
        public void OneDayWithSpaces()
        {
            var schedule = new Schedule(CalculusRequisition, ClassRooms);
            schedule.AddMeeting(schedule.GetMeetingsToAdd()
                .First(m => m.WeekType == WeekType.All && m.MeetingTime!.TimeSlotIndex == 1));
            while (schedule.NotUsedMeetings.Count != 0)
            {
                var meeting = schedule.GetMeetingsToAdd().First();
                schedule.AddMeeting(meeting);
            }

            Assert.Less(estimator.Estimate(schedule), 0); //one space per 2 weeks
        }

        [Test]
        public void MeetingsSpacesCountTest()
        {
            var day = new Meeting[7];
            Assert.Zero(day.GetMeetingsSpacesCount());
            day[1] = new (Oop, MeetingType.Lecture, OopTeacher2, WeekType.All,
                new (OopLab, "ФИИТ-101", "пн 5-6 пара", OopTeacher2));
            Assert.Zero(day.GetMeetingsSpacesCount());
            day[2] = day[1];
            Assert.Zero(day.GetMeetingsSpacesCount());
            day[4] = day[1];
            Assert.AreEqual(1, day.GetMeetingsSpacesCount());
            day[6] = day[1];
            Assert.AreEqual(2, day.GetMeetingsSpacesCount());
        }
    }
}