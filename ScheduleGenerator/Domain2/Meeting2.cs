using System.Text;
using CommonDomain;
using CommonDomain.Enums;

namespace Domain2;

/// <summary>
/// Модель пары
/// </summary>
/// <param name="Discipline">Предмет</param>
/// <param name="MeetingType">Вид пары: Лекция, практика, т.д</param>
/// <param name="Teacher">Преподаватель</param>
/// <param name="Groups">Группы, которые придут на пары</param>
/// <param name="Place">Место проведения</param>
/// <param name="RoomSpecs">Требования на аудиторию</param>
/// <param name="Duration">Продолжительность или количество одинаковых пар подряд</param>
/// <param name="WeekTypeSpec">Требование четности для пары (в большенсвте случаев оно будет либо All, либо OddOrEven)</param>
/// <param name="MeetingTimePriorities">Удобство для преподователя, может содержать четность</param>
/// <param name="After">Требование идти после пары этого предмета конкретного типа</param>
/// <param name="Priority">Задает порядок постановки пар, чем меньше, тем раньше</param>
/// <param name="IsFixed">Нужно ли использоавать положение данное предыдущим запуском алгоритма</param>
/// <param name="Ignore">Если true, то не должен ставится алгоритмом</param>
/// <param name="ClassRoom">Зафиксированная аудитория</param>
/// <param name="MeetingTime">Время которое поставил алгоритм на предыдущей итерации</param>
public record Meeting2(
    int Id,
    Discipline Discipline,
    MeetingType MeetingType,
    Teacher Teacher,
    List<int> Groups,
    string Place,
    RoomSpec[] RoomSpecs,
    int Duration,
    WeekType WeekTypeSpec,
    List<List<MeetingTime>> MeetingTimePriorities,
    MeetingType? After,
    int Priority,
    bool IsFixed,
    bool Ignore,
    string? ClassRoom,
    MeetingTime? MeetingTime
)
{
    public bool ShouldBePlaced => !Ignore;

    public override string ToString()
    {
        var stringBuilder = new StringBuilder(Discipline.Name);
        if (ClassRoom != null)
        {
            stringBuilder.Append($", {ClassRoom}");
        }

        if (MeetingTime!.WeekType != WeekType.All)
        {
            stringBuilder.Append(GetWeekTypeStringPart());
        }

        return stringBuilder.ToString();
    }

    public string GetWeekTypeStringPart()
    {
        return $" по {MeetingTime?.WeekType.ToRuString()} неделям";
    }
}