namespace solverTest.Models
{
    public class ScheduleItem
    {
        public int AgentId { get; set; }
        public string AgentType { get; set; }
        public int PalletIndex { get; set; }
        public string ItemId { get; set; }
        public double Start { get; set; }
        public double End { get; set; }
        public double WaitingTime { get; set; }
        public double Energy { get; set; }
        public int Quantity { get; set; } 

    }

}
