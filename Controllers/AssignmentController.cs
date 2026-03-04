using Google.OrTools.LinearSolver;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using solverTest.Models;
using solverTest.Scheduler;
namespace solverTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssignmentController : ControllerBase
    {
        
        [HttpPost]
        public IActionResult Solve([FromBody] AssignmentRequest req)
        {
            
            // 0) Request Validation
            if (req.A.Length != req.NumItems || req.T.Length != req.NumItems)
                return BadRequest("Dimension mismatch: items");

            if (req.A[0].Length != req.NumAgents || req.T[0].Length != req.NumAgents)
                return BadRequest("Dimension mismatch: agents");

           
            // 1) Load Solver
            Solver solver = Solver.CreateSolver("CBC_MIXED_INTEGER_PROGRAMMING");
            solver.SetTimeLimit(10000);


            /*MPSolverParameters parameters = new MPSolverParameters();   
            parameters.SetDoubleParam(MPSolverParameters.DoubleParam.RELATIVE_MIP_GAP, 0.0);
            
            bool aaa= solver.SetSolverSpecificParametersAsString(parameters.ToString());
            */
            int I = req.NumItems;
            int J = req.NumAgents;

            // 2) Variables
            Variable[,] x = new Variable[I, J];

            for (int i = 0; i < I; i++)
                for (int j = 0; j < J; j++)
                    x[i, j] = solver.MakeBoolVar($"x_{i}_{j}");

            Variable Cmax = solver.MakeNumVar(0, double.PositiveInfinity, "Cmax");

            
            // 3) Constraint (2): assignment
            for (int i = 0; i < I; i++)
            {
                Constraint c = solver.MakeConstraint(1, 1, $"assign_item_{i}");
                for (int j = 0; j < J; j++)
                    c.SetCoefficient(x[i, j], req.A[i][j]);
            }


            // 4) Constraint (3): makespan
            for (int j = 0; j < J; j++)
            {
                Constraint c = solver.MakeConstraint(0, double.PositiveInfinity, $"makespan_agent_{j}");
                c.SetCoefficient(Cmax, 1);

                for (int i = 0; i < I; i++)
                    c.SetCoefficient(x[i, j], -req.T[i][j]);
            }

            
            // 5) Objective function: Minimize Cmax
            solver.Objective().SetCoefficient(Cmax, 1);
            solver.Objective().SetMinimization();

            // 6) Solve
            var status = solver.Solve();

            if (status != Solver.ResultStatus.OPTIMAL && status != Solver.ResultStatus.FEASIBLE)
            {
                return BadRequest(new
                {
                    Status = status.ToString(),
                    Message = "No optimal solution found"
                });
            }

            // 7) Response
            int[][] X = new int[I][];
            for (int i = 0; i < I; i++)
            {
                X[i] = new int[J];
                for (int j = 0; j < J; j++)
                    X[i][j] = (int)x[i, j].SolutionValue();
            }

            return Ok(new AssignmentResponse
            {
                Status = status.ToString(),
                Cmax = Math.Round(Cmax.SolutionValue(), 3),
                X = X
            });
        }
        
      
    }
}
