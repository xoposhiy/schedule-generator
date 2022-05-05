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
        var state = new State(Array.Empty<Meeting2>());
        Assert.IsEmpty(state[FirstMeetingTime]);
        var meetingToPlace = FirstMeeting with {MeetingTime = FirstMeetingTime};
        state.PlaceMeeting(meetingToPlace);
        Assert.Contains(meetingToPlace, state[FirstMeetingTime].ToList());
        Assert.AreEqual(state[FirstMeetingTime].Count(), 1);
    }

    [Test]
    public static void StateIndexerIgnoreMeetingTest()
    {
        var state = CreateEmptyState();
        Assert.IsEmpty(state[FirstMeetingTime]);
        var ignoreMeeting = FirstMeeting with {MeetingTime = FirstMeetingTime, Ignore = true};
        state.PlaceMeeting(ignoreMeeting);
        Assert.IsEmpty(state[FirstMeetingTime]);
    }

    [Test]
    public static void StateIndexerSelectCorrectDay()
    {
        var state = CreateEmptyState();
        var secondMeetingTime = FirstMeetingTime with {DayOfWeek = DayOfWeek.Wednesday};
        state.PlaceMeeting(FirstMeeting with {MeetingTime = FirstMeetingTime});
        state.PlaceMeeting(FirstMeeting with {MeetingTime = secondMeetingTime});
        Assert.That(state[secondMeetingTime].Count() == 1);
    }

    [Test]
    public static void StateIndexerSelectCorrectWeekType()
    {
        var state = CreateEmptyState();
        var odd = FirstMeetingTime with {WeekType = WeekType.Odd};
        var even = FirstMeetingTime with {WeekType = WeekType.Even};
        state.PlaceMeeting(FirstMeeting with {MeetingTime = odd});
        state.PlaceMeeting(FirstMeeting with {MeetingTime = even});
        Assert.AreEqual(2, state[FirstMeetingTime].Count());
        Assert.AreEqual(1, state[even].Count());
        Assert.AreEqual(1, state[odd].Count());
        Assert.AreNotEqual(state[odd], state[even]);
    }


    [Test]
    public static void StateCopyTest()
    {
        var state = CreateEmptyState();
        var secondMeetingTime = FirstMeetingTime with {DayOfWeek = DayOfWeek.Wednesday};
        var emptyCopy = state.Copy();
        state.PlaceMeeting(FirstMeeting with {MeetingTime = FirstMeetingTime});
        state.PlaceMeeting(FirstMeeting with {MeetingTime = secondMeetingTime});
        var copy = state.Copy();
        Assert.IsEmpty(emptyCopy.PlacedMeetings);
        Assert.True(state.PlacedMeetings.SequenceEqual(copy.PlacedMeetings));
    }
}