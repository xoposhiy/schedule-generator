namespace Domain2.Algorithms.Estimators;

/// <summary>
/// Умеет оценивать пары в контексте расписания
/// </summary>
public interface IMeetingEstimator
{
    /// <summary>
    /// Дает оценку паре
    /// </summary>
    /// <param name="state">Рассписание, в которое будет поставлена пара</param>
    /// <param name="meeting">Пара, которая будет поставлена</param>
    /// <returns>Оценка, чем выше, тем лучше</returns>
    double EstimateMeeting(State state, Meeting2 meeting);
}