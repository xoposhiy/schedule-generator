namespace Domain.MeetingsParts
{
    public record Discipline(string Name)
    {
        public override string ToString()
        {
            return Name;
        }
    }
}