namespace solverTest.Models
{
    public class SchedulerRequest
    {
        public List<Agent> Agents { get; set; }
        public List<PalletRoute> Routes { get; set; }
        public Dictionary<int, List<int>> Assignment { get; set; }

        public int Iterations { get; set; }
        public Dictionary<string, double> ItemWeight { get; set; }

    }
}
