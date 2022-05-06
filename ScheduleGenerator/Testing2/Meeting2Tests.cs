using CommonDomain.Enums;
using NUnit.Framework;
using static Testing2.ObjectMother2;

namespace Testing2;

[TestFixture]
public class Meeting2Tests
{
    [Test]
    public void ToStringTest()
    {
        var meeting = FirstMeeting with {MeetingTime = FirstMeetingTime};
        var simpleString = meeting.ToString();
        var complicatedString = (meeting with {ClassRoom = "512", WeekTypeSpec = WeekType.Odd}).ToString();
        Assert.AreNotEqual(simpleString, complicatedString);
    }

    [Test]
    public void ToStringTrowsWhenMeetingTimeIsNull()
    {
        // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
        Assert.Throws<NullReferenceException>(() => FirstMeeting.ToString());
    }
}