using CommonDomain.Enums;

namespace Domain2;

public static class Domain2Extensions
{
    public static string ToRuString(this WeekType weekType)
    {
        if (weekType == WeekType.Even) return "чётным";
        if (weekType == WeekType.Odd) return "нечётным";
        return weekType.ToString();
    }
}