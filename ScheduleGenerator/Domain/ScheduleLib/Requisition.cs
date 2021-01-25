namespace Domain.ScheduleLib
{
    public class Requisition
    {
        public RequisitionItem[] Items;

        public Requisition(RequisitionItem[] items)
        {
            Items = items;
        }
    }
}