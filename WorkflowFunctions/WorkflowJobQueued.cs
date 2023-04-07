using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace WorkflowFunctions
{
    public static class WorkflowJobQueued
    {
        [FunctionName("WorkflowJobQueued")]
        [return: Queue("workflow-job-queued", Connection = "AzureWebJobsStorage")]
        public static async Task<string> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequest req,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            return requestBody;
        }
    }
}