namespace Domain2.Algorithms;

public class MeanStudentIntersectionEstimator : IEstimator
{
    private const int SufferingStudentsImportance = 100;

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
        var sufferingStudents = currentMeetings.Sum(m => probabilityStorage.GetCommonStudents(meeting, m));
        var previousMeetings = state[meetingTime with {TimeSlot = meetingTime.TimeSlot - 1}];
        var nextMeetings = state[meetingTime with {TimeSlot = meetingTime.TimeSlot + meeting.Duration}];
        var satisfiedStudents = previousMeetings.Concat(nextMeetings)
            .Where(m => !currentMeetings.Contains(m))
            .Sum(m => probabilityStorage.GetCommonStudents(meeting, m));
        var score = satisfiedStudents - SufferingStudentsImportance * sufferingStudents;
        var normalizationConstant = SufferingStudentsImportance * probabilityStorage.StudentsCount;
        return score / normalizationConstant;
    }
}