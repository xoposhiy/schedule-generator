using Domain.Enums;

namespace Domain.MeetingsParts
{
    public record Room(string Name, RoomSpec[] Specs);
}