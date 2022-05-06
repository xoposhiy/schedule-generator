using CommonDomain.Enums;
using NUnit.Framework;
using static Testing2.ObjectMother2;

namespace Testing2;

public static class StateTests
{
    [Test]
    public static void AddMeetingTest()
    {
        var state = CreateEmptyState(FirstMeeting);
        Assert.AreEqual(1, state.NotPlacedMeetings.Count);
        Assert.IsEmpty(state.PlacedMeetings);
        state = state.AddMeeting(FirstMeeting);
        Assert.IsEmpty(state.NotPlacedMeetings);
        Assert.IsNotEmpty(state.PlacedMeetings);
    }

    [Test]
    public static void StateIndexerTest()
    {
        var state = CreateEmptyState(FirstMeeting);
        Assert.IsEmpty(state[FirstMeetingTime]);
        state = state.AddMeeting(FirstMeeting);
        Assert.Contains(FirstMeeting, state[FirstMeetingTime].ToList());
        Assert.AreEqual(state[FirstMeetingTime].Count(), 1);
    }

    [Test]
    public static void StateIndexerIgnoreMeetingTest()
    {
        var ignoreMeeting = FirstMeeting with {Ignore = true};
        var state = CreateEmptyState(ignoreMeeting);
        Assert.IsEmpty(state[FirstMeetingTime]);
        state = state.AddMeeting(ignoreMeeting);
        Assert.IsEmpty(state[FirstMeetingTime]);
    }

    [Test]
    public static void StateIndexerSelectCorrectDay()
    {
        var secondMeeting = FirstMeeting with {Id = 1, MeetingTime = SecondMeetingTime};
        var state = CreateEmptyState(FirstMeeting, secondMeeting);
        state = state.AddMeeting(FirstMeeting);
        state = state.AddMeeting(secondMeeting);
        Assert.That(state[FirstMeetingTime].Count() == 1);
        Assert.That(state[SecondMeetingTime].Count() == 1);
    }

    [Test]
    public static void StateIndexerSelectCorrectWeekType()
    {
        var odd = FirstMeetingTime with {WeekType = WeekType.Odd};
        var even = FirstMeetingTime with {WeekType = WeekType.Even};
        var oddMeeting = FirstMeeting with {MeetingTime = odd};
        var evenMeeting = FirstMeeting with {Id = 1, MeetingTime = even};
        var state = CreateEmptyState(oddMeeting, evenMeeting);
        state = state.AddMeeting(oddMeeting);
        state = state.AddMeeting(evenMeeting);
        Assert.AreEqual(2, state[FirstMeetingTime].Count());
        Assert.AreEqual(1, state[even].Count());
        Assert.AreEqual(1, state[odd].Count());
        Assert.AreNotEqual(state[odd], state[even]);
    }


    [Test]
    public static void StateCopyTest()
    {
        var secondMeeting = FirstMeeting with {Id = 1, MeetingTime = SecondMeetingTime};
        var state = CreateEmptyState(FirstMeeting, secondMeeting);
        var firstState = state.AddMeeting(FirstMeeting);
        var secondState = firstState.AddMeeting(secondMeeting);
        Assert.IsEmpty(state.PlacedMeetings);
        Assert.False(firstState.PlacedMeetings.SequenceEqual(secondState.PlacedMeetings));
    }
}