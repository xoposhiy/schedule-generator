namespace CommonDomain
{
    public record Teacher(string Name)
    {
        public override string ToString()
        {
            return Name;
        }
    }
}