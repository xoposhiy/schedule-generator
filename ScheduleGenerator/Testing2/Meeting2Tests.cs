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
        var time = FirstMeetingTime with {WeekType = WeekType.Odd};
        var meeting = FirstMeeting with {MeetingTime = time};
        var simpleString = meeting.ToString();
        var complicatedString = (meeting with {ClassRoom = "512"}).ToString();
        Assert.AreNotEqual(simpleString, complicatedString);
    }

    [Test]
    public void ToStringTrowsWhenMeetingTimeIsNull()
    {
        var dangerousMeeting = FirstMeeting with {MeetingTime = null};
        Assert.Throws<NullReferenceException>(() => Console.WriteLine(dangerousMeeting.ToString()));
    }
}