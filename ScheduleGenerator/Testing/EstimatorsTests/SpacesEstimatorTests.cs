using System.Linq;
using Domain;
using Domain.Algorithms;
using Domain.Algorithms.Estimators;
using Domain.Enums;
using NUnit.Framework;
using static Testing.ObjectMother;

namespace Testing.EstimatorsTests
{
    [TestFixture]
    public class SpacesEstimatorTests
    {
        private readonly IEstimator studentEstimator = new StudentsSpacesEstimator();
        private readonly IEstimator teacherEstimator = new TeacherSpacesEstimator();

        [Test]
        public void OneDayWithSpaces()
        {
            var schedule = new Schedule(CalculusRequisition, ClassRooms);
            schedule.AddMeeting(schedule.GetMeetingsToAdd()
                .First(m => m.WeekType == WeekType.All && m.MeetingTime!.TimeSlot == 1));
            while (schedule.NotUsedMeetings.Count != 0)
            {
                var meeting = schedule.GetMeetingsToAdd().First();
                schedule.AddMeeting(meeting);
            }

            Assert.Negative(studentEstimator.Estimate(schedule)); //one space per 2 weeks
            Assert.Negative(teacherEstimator.Estimate(schedule));
        }

        [Test]
        public void MeetingsSpacesCountTest()
        {
            var day = new Meeting[7];
            Assert.Zero(day.GetMeetingsSpacesCount());
            day[1] = new(WeekType.All, OopLabRequisitionItem);
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