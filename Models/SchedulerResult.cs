namespace solverTest.Models
{
    public class SchedulerResult
    {
        public List<ScheduleItem> BestSchedule { get; set; }
        public double Makespan { get; set; }
        public double AverageHumanEnergy { get; set; }
        public Dictionary<int, double> HumanEnergyPerAgent { get; set; }
    }

}
