using CommonDomain.Enums;

namespace CommonDomain
{
    public record Discipline(string Name, DisciplineType Type = DisciplineType.Obligatory)
    {
        public override string ToString()
        {
            return Name;
        }
    }
}