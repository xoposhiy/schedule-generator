namespace Domain2;

public static class Constants
{
    /// <summary>
    /// Верхнее ограничении на количесво пар в день
    /// </summary>
    public const int TimeSlots = 9;

    /// <summary>
    /// Количество столбцов в описании пары
    /// </summary>
    public const int FormattedMeetingsRowWidth = 16;

    /// <summary>
    /// Значение по умолчанию для отсутствия приоритета
    /// </summary>
    public const int UnselectedPriority = 5;

    /// <summary>
    /// Путь до json-а из лк УРФУ, с зачислен
    /// </summary>
    public const string DistributionJsonPath = "Probabilities/students_distribution.json";
    public const string PrioritiesJsonPath = "Probabilities/StudentMupPriorities.json";
}