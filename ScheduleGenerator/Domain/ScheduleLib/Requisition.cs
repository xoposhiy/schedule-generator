using System.Linq;

namespace ScheduleLib
{
    public class Requisition
    {
        public LearningPlanItem PlanItem;
        
        /* TODO: Тут нужно, чтобы в коде было понятно разделены следующие кейсы:
        "ФИИТ-101, ФИИТ-102" — означает, что эти две группы имеют одинаковый приоритет
        "ФИИТ-101+ФИИТ-102 — означает, что это поток из двух групп (видимо, лекционный).
        "ФИИТ-101 + ФИИТ-102, ФИИТ-103 + ФИИТ-104 — означает, что это либо поток из 101,102, либо поток из 103,104 и они имеют один приоритет. Тут логичнее приоритет сделать больше именно у +.
        "ФИИТ-1*+ФИИТ-1*" означает, что это поток из любых двух групп ФИИТа будет эквивалентен по приоритету.

        Надо убрать парсинг и сплит по плюсикам, а сделать объектную модель, которая представляет всё это многообразие вариантов.
         */
        public GroupRequisition[] GroupPriorities;

        //TODO: Нужно более строго смоделировать, что это либо комп-класс, либо класс с проектором, либо конкретный номер аудитории.
        public string? Location;
        public int RepetitionsCount;
        
        // TODO: Нужен способ сказать, что некоторые времена одинаково приоритетны.
        // Например, как выразить, что в любой день недели первой парой - норм, второй - менее удобно, а в любое другое время - не удобно.
        public MeetingTime[] MeetingTimePriorities;
        public Teacher Teacher;

        public Requisition(LearningPlanItem planItem, GroupRequisition[] groupPriorities, string? location, int repetitionsCount, MeetingTime[] meetingTimePriorities, Teacher teacher)
        {
            PlanItem = planItem;
            GroupPriorities = groupPriorities;
            Location = location;
            RepetitionsCount = repetitionsCount;
            MeetingTimePriorities = meetingTimePriorities;
            Teacher = teacher;
        }
    }

    public class GroupRequisition
    {
        public string[] Groups;

        public GroupRequisition(string priority)
        {
            Groups = priority.Split('+').Select(s => s.Trim()).ToArray();
        }
    }
}
