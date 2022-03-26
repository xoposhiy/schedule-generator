using CommonDomain.Enums;

namespace CommonDomain
{
    public record Room(string Name, RoomSpec[] Specs);
}