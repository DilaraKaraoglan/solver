namespace solverTest.Models
{
    public class AssignmentResponse
    {
        public string Status { get; set; }
        public double Cmax { get; set; }

        // x_ij çözümü
        public int[][] X { get; set; }
    }
}
