using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using solverTest.Models;
using solverTest.Scheduler;

namespace solverTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SchedulerController : ControllerBase
    {

        [HttpPost("RandomSearchSchedule")]
        public IActionResult RandomSearchSchedule([FromBody] SchedulerRequest request)
        {
            var scheduler = new RandomSearchScheduler(
                request.Agents,
                request.Routes,
                request.Assignment,
                request.ItemWeight
            );

            var result = scheduler.Optimize(request.Iterations);

            return Ok(result);
        }




    }


}
