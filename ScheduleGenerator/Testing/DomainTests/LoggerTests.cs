using System.Linq;
using ApprovalTests;
using ApprovalTests.Reporters;
using Domain;
using NUnit.Framework;
using static Domain.DomainExtensions;
using static Testing.ObjectMother;

namespace Testing.DomainTests
{
    [TestFixture]
    [UseReporter(typeof(DiffReporter))]
    public class LoggerTests
    {
        [Test]
        public void CheckLogger()
        {
            var estimator = GetDefaultCombinedEstimator();
            var schedule = new Schedule(FullMondayRequisition, ClassroomsRequisitions);

            while (true)
            {
                var meetingToAdd = schedule.GetMeetingsToAdd().FirstOrDefault();
                if (meetingToAdd == null) break;
                schedule.AddMeeting(meetingToAdd);
            }

            using var logger = new Infrastructure.Logger("TestLogger");
            estimator.Estimate(schedule, logger);
            var loggerString = logger.ToString();
            Approvals.Verify(loggerString);
        }
    }
}