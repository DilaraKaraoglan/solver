namespace solverTest.Models
{
    public class AssignmentRequest
    {
        public int NumItems { get; set; }
        public int NumAgents { get; set; }

        // a_ij : uygunluk (0/1)
        public int[][] A { get; set; }

        // t_ij : süre
        public double[][] T { get; set; }
    }
}
