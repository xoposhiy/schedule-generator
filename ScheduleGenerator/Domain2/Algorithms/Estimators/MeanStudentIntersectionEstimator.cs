namespace Domain2.Algorithms.Estimators;

public class MeanStudentIntersectionEstimator : IMeetingEstimator
{
    private const double SufferingFromIntersection = 100;
    private const double SufferingFromDifferentLocations = 30;

    public double EstimateMeeting(State state, Meeting2 meeting)
    {
        if (meeting.IsFixed || meeting.Ignore) return 1;

        var meetingTime = meeting.MeetingTime!;
        var currentMeetings = new HashSet<Meeting2>();
        for (var i = 0; i < meeting.Duration; i++)
            currentMeetings.UnionWith(state[meetingTime with {TimeSlot = meetingTime.TimeSlot + i}]);

        if (currentMeetings.Any(e => e.Teacher == meeting.Teacher))
            return -1;

        var probabilityStorage = state.ProbabilityStorage;
        var intersectionStudents = currentMeetings.Sum(m => probabilityStorage.GetCommonStudents(meeting, m));
        var previousMeetings = state[meetingTime with {TimeSlot = meetingTime.TimeSlot - 1}];
        var nextMeetings = state[meetingTime with {TimeSlot = meetingTime.TimeSlot + meeting.Duration}];
        var locationSatisfaction = previousMeetings.Concat(nextMeetings)
            .Where(m => !currentMeetings.Contains(m))
            .Sum(m => probabilityStorage.GetCommonStudents(meeting, m) *
                      (m.Place == meeting.Place ? 1 : -SufferingFromDifferentLocations));
        var score = locationSatisfaction - SufferingFromIntersection * intersectionStudents;
        var normalizationConstant = Math.Max(SufferingFromIntersection, SufferingFromDifferentLocations) *
                                    probabilityStorage.StudentsCount;
        return score / normalizationConstant;
    }
}