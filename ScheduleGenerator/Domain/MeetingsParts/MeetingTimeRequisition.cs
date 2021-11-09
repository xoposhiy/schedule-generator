using System.Collections.Generic;

namespace Domain.MeetingsParts
{
    public record MeetingTimeRequisition(HashSet<MeetingTime> MeetingTimeChoices);
}