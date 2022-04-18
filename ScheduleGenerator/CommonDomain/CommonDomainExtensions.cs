using CommonDomain.Enums;

namespace CommonDomain;

public static class CommonDomainExtensions
{
    public static bool WeekTypesIntersects(WeekType first, WeekType second)
    {
        return first == WeekType.All || second == WeekType.All || first == second;
    }
}