using CommonDomain.Enums;
using Domain2;
using NUnit.Framework;
using static Domain2.SheetToRequisitionConverter;

namespace Testing2;

[TestFixture]
public class MeetingTimeParseTests
{
    private static string TestFormatMeetingTimes(List<List<MeetingTime>> priorities)
    {
        var meetingTimeRequisitions = priorities.Select(p =>
            string.Join(", ", p.Select(m => $"{m.DayOfWeek.ToString()[..2]}{m.TimeSlot}")));
        return string.Join(";", meetingTimeRequisitions);
    }

    [TestCase("пн 1-3 \n пт 4-6", "Mo1, Mo2, Mo3;Fr4, Fr5, Fr6")]
    [TestCase("пн 1 \n вт 5", "Mo1;Tu5")]
    [TestCase("пн, вт 1, 2, вт 4", "Mo1, Mo2, Tu1, Tu2, Tu4")]
    [TestCase("пн 5, вт 3", "Mo5, Tu3")]
    [TestCase("пт-сб 5-6", "Fr5, Fr6, Sa5, Sa6")]
    [TestCase("пн 1-2,4-5", "Mo1, Mo2, Mo4, Mo5")]
    [TestCase("чт 1,2,3", "Th1, Th2, Th3")]
    [TestCase("пн,вт,ср 1", "Mo1, Tu1, We1")]
    [TestCase("ср 2, 3-5", "We2, We3, We4, We5")]
    [TestCase("чт 2-4", "Th2, Th3, Th4")]
    [TestCase("сб 2, 3", "Sa2, Sa3")]
    [TestCase("пн, ср-чт 6", "Mo6, We6, Th6")]
    [TestCase("чт-сб 4", "Th4, Fr4, Sa4")]
    [TestCase("пн, вт 3", "Mo3, Tu3")]
    [TestCase("пн 1-6", "Mo1, Mo2, Mo3, Mo4, Mo5, Mo6")]
    [TestCase("пн 1", "Mo1")]
    public void MeetingTimeParseTest(string rawMeetingTime, string expected)
    {
        var meetingTimeRequisitions = TestFormatMeetingTimes(ParseMeetingTimePriorities(rawMeetingTime));
        var actual = string.Join(";", meetingTimeRequisitions);
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void EmptyMeetingTimeParseTest()
    {
        var actual = TestFormatMeetingTimes(ParseMeetingTimePriorities(""));
        var expected = TestFormatMeetingTimes(new List<List<MeetingTime>>() {GetAllPossibleMeetingTimes(WeekType.All)});
        Assert.AreEqual(expected, actual);
    }

    [TestCase("пн 1 пара")] //лишних слов больше использовать нельзя
    [TestCase("вт: 7")] //старый формат
    [TestCase("пн 0")]
    [TestCase("пн 9999")]
    [TestCase("erdtcyvbnm 4")]
    [TestCase("пг, сб 2")]
    [TestCase("1-2")]
    [TestCase("вт, 1-5 \n пт, 3-6")]
    [TestCase("пн-пт, 3-5")]
    [TestCase("пн-пт; 3-4")]
    [TestCase("пн-пт; 3-4")]
    [TestCase("пара")]
    [TestCase("пн")] //Так пока нельзя. возможно потом стоит сделать чтобы было можно
    public void WrongTimeRequisitionFormatShouldNotWork(string rawTimeRequisition)
    {
        Assert.Throws(Is.InstanceOf<Exception>(),
            () => ParseMeetingTimePriorities(rawTimeRequisition));
    }
}