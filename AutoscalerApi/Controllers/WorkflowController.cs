using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AutoscalerApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AutoscalerApi.Controllers;

[ApiController]
[Route("[controller]")]
public class WorkflowController : ControllerBase
{
    private readonly IDockerService _dockerService;
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(IDockerService dockerService, ILogger<WorkflowController> logger)
    {
        _dockerService = dockerService;
        _logger = logger;
    }

    [HttpGet("ping")]
    public IActionResult Get()
    {
        return Ok(new { message = "Pong" });
    }

    [HttpPost("workflow-trigger")]
    public async Task<IActionResult> Post([FromBody] Workflow? workflow)
    {
        _logger.LogInformation($"Workflow hook '{workflow.action}' on repo '{workflow.repository.FullName}' received");
        if (workflow == null) return Ok();
        
        _logger.LogInformation($"Executing workflow");
        await _dockerService.ProcessWorkflow(workflow);

        return Ok();
    }
}
