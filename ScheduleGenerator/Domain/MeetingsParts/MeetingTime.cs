using System;

namespace Domain.MeetingsParts
{
    public record MeetingTime(DayOfWeek Day, int TimeSlotIndex)
    {
        public override string ToString()
        {
            return $"Day: {Day}, TimeSlotIndex: {TimeSlotIndex}";
        }
    }
}