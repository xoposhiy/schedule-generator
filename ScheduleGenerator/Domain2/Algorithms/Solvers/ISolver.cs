using CommonInfrastructure;

namespace Domain2.Algorithms.Solvers;

/// <summary>
/// Заполняет рассписание
/// </summary>
public interface ISolver
{
    /// <summary>
    /// Получает на вход пустое рассписание и за лимит времени выдает последовательность рассписаний
    /// </summary>
    /// <param name="problem">Рассписание, которое надо заполнить</param>
    /// <param name="countdown">Счетчик времени</param>
    /// <returns>Полседовательность расписаний с оценкой</returns>
    IEnumerable<(State schedule, double score)> GetSolutions(State problem, Countdown countdown);
}