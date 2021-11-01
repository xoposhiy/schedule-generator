using System;

namespace Domain.MeetingsParts
{
    public record MeetingTime(DayOfWeek Day, int TimeSlot)
    {
        public override string ToString()
        {
            return $"Day: {Day}, TimeSlotIndex: {TimeSlot}";
        }
    }
}