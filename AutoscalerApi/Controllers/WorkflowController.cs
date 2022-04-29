using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AutoscalerApi.Models;
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
        return Ok(new {message = "Pong"});
    }

    [HttpPost("workflow-trigger")]
    public async Task<IActionResult> Post([FromBody] Workflow? workflow)
    {
        if (workflow == null) return Ok();
        _logger.LogInformation("Workflow hook '{Action}' on repo '{FullName}' received", workflow.Action, workflow.Repository.FullName);
        
        await _dockerService.ProcessWorkflow(workflow);
        return Ok();
    }
}
