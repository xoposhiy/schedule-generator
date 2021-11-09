namespace Domain.MeetingsParts
{
    public record GroupRequisition(GroupsChoice[] GroupsChoices)
    {
        public override string ToString()
        {
            return string.Join<GroupsChoice>("; ", GroupsChoices);
        }
    }
}