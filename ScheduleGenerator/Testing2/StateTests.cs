using CommonDomain.Enums;
using Domain2;
using NUnit.Framework;
using static Testing2.ObjectMother2;

namespace Testing2;

public static class StateTests
{
    [Test]
    public static void AddMeetingTest()
    {
        var state = new State(new List<Meeting2> {FirstMeeting});
        Assert.AreEqual(1, state.NotPlacedMeetings.Count);
        Assert.IsEmpty(state.PlacedMeetings);
        state.PlaceMeeting(FirstMeeting);
        Assert.IsEmpty(state.NotPlacedMeetings);
        Assert.IsNotEmpty(state.PlacedMeetings);
    }

    [Test]
    public static void StateIndexerTest()
    {
        var meetingTime = new MeetingTime(WeekType.All, DayOfWeek.Monday, 2);
        var state = new State(Array.Empty<Meeting2>());
        Assert.IsEmpty(state[meetingTime]);
        var meetingToPlace = FirstMeeting with {MeetingTime = meetingTime};
        state.PlaceMeeting(meetingToPlace);
        Assert.Contains(meetingToPlace, state[meetingTime].ToList());
        Assert.AreEqual(state[meetingTime].Count(), 1);
    }
}