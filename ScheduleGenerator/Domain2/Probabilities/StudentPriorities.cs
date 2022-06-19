namespace Domain2;

/// <summary>
/// Приоритет студента для дисциплины
/// </summary>
/// <param name="FormPriority">Приоритет, оставленный в форме</param>
/// <param name="OfficialPriority">Приорите в личном кабинете</param>
/// <param name="Enlisted">Факт зачисления</param>
public record StudentPriorities(int FormPriority, int OfficialPriority, bool Enlisted);