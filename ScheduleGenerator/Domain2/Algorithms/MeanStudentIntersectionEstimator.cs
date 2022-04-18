namespace Domain2.Algorithms;

public class MeanStudentIntersectionEstimator : IEstimator
{
    public double EstimateMeeting(State state, Meeting2 meeting)
    {
        var meetingTime = meeting.MeetingTime!;
        var currentMeetings = new List<Meeting2>();
        for (var i = 0; i < meeting.Duration; i++)
        {
            currentMeetings.AddRange(state[meetingTime with {TimeSlot = meetingTime.TimeSlot + i}]);
        }

        var sufferingStudents = currentMeetings.Sum(meeting.GetCommonStudents);
        var previousMeetings = state[meetingTime with {TimeSlot = meetingTime.TimeSlot - 1}];
        var nextMeetings = state[meetingTime with {TimeSlot = meetingTime.TimeSlot + meeting.Duration}];
        var satisfiedStudents = previousMeetings.Concat(nextMeetings)
            .Where(m => !currentMeetings.Contains(m))
            .Sum(meeting.GetCommonStudents);
        return satisfiedStudents - 100 * sufferingStudents;
    }
}