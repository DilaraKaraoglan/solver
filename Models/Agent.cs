namespace solverTest.Models
{
    public class Agent
    {
        public int Id { get; set; }
        public string Type { get; set; } // human / robot
        public double Speed { get; set; }
        public double PickTime { get; set; }
    }
}
